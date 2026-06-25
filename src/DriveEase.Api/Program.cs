using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using DriveEase.Api.Auth;
using DriveEase.Api.Messaging;
using DriveEase.Api.Middleware;
using DriveEase.Api.Workers;
using DriveEase.Enrollments.Infrastructure;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Notifications.Infrastructure;
using DriveEase.Schools.Infrastructure;
using DriveEase.Schools.Infrastructure.Persistence;
using DriveEase.Shared;
using DriveEase.Shared.Behaviors;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Telemetry;
using DriveEase.Students.Application;
using DriveEase.Students.Infrastructure;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
// Replaces default ILogger. Reads config from "Serilog" section in appsettings.
// Enriches every log line with CorrelationId, MachineName, and Environment.
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "DriveEase.Api")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// Limit request body size globally — prevents memory exhaustion from oversized payloads
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 1_048_576); // 1 MiB

// ── IClock ────────────────────────────────────────────────────────────────────
// Singleton so tests can substitute a fake clock to control time-sensitive logic.
builder.Services.AddSingleton<IClock, SystemClock>();

// ── JwtOptions binding ────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<RefreshTokenService>();

// ── Password hasher ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// ── GlobalExceptionHandler + ProblemDetails ───────────────────────────────────
// Catches all unhandled exceptions and returns RFC 7807 ProblemDetails JSON.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Authentication — Entra ID (school admins) + local JWT (students) ──────────
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
builder.Services.AddAuthentication()
    .AddJwtBearer("StudentBearer", opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ── Authorization policies ────────────────────────────────────────────────────
// Default policy accepts either Entra ID ("Bearer") or local student JWT ("StudentBearer").
// Role policies restrict specific endpoints to the right callers.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "StudentBearer")
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("Student", policy =>
        policy.AddAuthenticationSchemes("StudentBearer")
              .RequireRole("Student"));

    options.AddPolicy("SchoolAdmin", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
              .RequireRole("SchoolAdmin"));

    options.AddPolicy("Instructor", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
              .RequireRole("Instructor"));
});

// ── API versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── HybridCache ───────────────────────────────────────────────────────────────
// L1: in-process memory. L2: Redis when configured (falls back to L1-only in dev).
// Stampede protection is built-in — only one request populates the cache on a miss.
builder.Services.AddHybridCache(opts =>
{
    opts.MaximumPayloadBytes = 1024 * 1024; // 1 MiB per entry
    opts.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Swagger with bearer auth scheme ──────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DriveEase API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In          = ParameterLocation.Header,
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        Description = "Entra ID token or student JWT from /auth/login",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Observability (OpenTelemetry → Azure App Insights) ────────────────────────
var appInsightsConnStr = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

var appInsightsResolved = !string.IsNullOrWhiteSpace(appInsightsConnStr)
    && !appInsightsConnStr.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

var otelBuilder = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DriveEaseTelemetry.ServiceName)
        .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;
            opts.RecordException = true;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(DriveEaseTelemetry.ServiceName));

if (appInsightsResolved)
    otelBuilder.UseAzureMonitor(opts => opts.ConnectionString = appInsightsConnStr!);

// ── Database ──────────────────────────────────────────────────────────────────
var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");

var sqlResolved = !string.IsNullOrWhiteSpace(sqlConn) &&
                  !sqlConn.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

if (sqlResolved)
{
    builder.Services
        .AddEnrollmentsModule(sqlConn!)
        .AddSchoolsModule(sqlConn!)
        .AddStudentsModule(sqlConn!)
        .AddLessonsModule(sqlConn!);
}
else
{
    static string DbPath(string module) =>
        $"Data Source={Path.Combine(Path.GetTempPath(), $"driveease-{module}.db")}";
    builder.Services
        .AddEnrollmentsModule(DbPath("enrollments"))
        .AddSchoolsModule(DbPath("schools"))
        .AddStudentsModule(DbPath("students"))
        .AddLessonsModule(DbPath("lessons"));
}

// ── Event bus ─────────────────────────────────────────────────────────────────
var sbNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

var sbResolved = !string.IsNullOrWhiteSpace(sbNamespace) &&
                 !sbNamespace.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

if (sbResolved)
    builder.Services.AddSingleton<IEventBus>(new AzureServiceBusEventBus(sbNamespace!));
else
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ── MediatR + pipeline behaviors ─────────────────────────────────────────────
// ValidationBehavior runs FluentValidation before every handler.
// LoggingBehavior logs request name and outcome for every command/query.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Enrollments.Application.Commands.EnrollStudent.EnrollStudentHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Schools.Application.Commands.RegisterSchool.RegisterSchoolHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Students.Application.Commands.RegisterStudent.RegisterStudentHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Lessons.Application.Commands.BookLesson.BookLessonHandler).Assembly);

    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddNotificationsModule();
builder.Services.AddHostedService<OutboxRelayWorker>();

var app = builder.Build();

// ── Exception handler (must be first in pipeline) ────────────────────────────
app.UseExceptionHandler();

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// ── Correlation ID middleware ─────────────────────────────────────────────────
// Reads X-Correlation-Id from request (or generates one) and pushes to Serilog.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRateLimiter();

// ── JWT structural pre-check (zero allocation) ───────────────────────────────
app.Use(async (ctx, next) =>
{
    var raw = ctx.Request.Headers.Authorization.ToString();
    if (raw.Length > 7)
    {
        ReadOnlySpan<char> header = raw.AsSpan();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnlySpan<char> token = header.Slice(7);
            int dots = 0;
            foreach (char c in token)
                if (c == '.') dots++;
            if (dots != 2)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        }
    }
    await next();
});

// ── Schema init on cold start ─────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    if (sqlResolved)
    {
        var ctx = sp.GetRequiredService<SchoolsDbContext>();
        var conn = ctx.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'schools')    EXEC('CREATE SCHEMA schools');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'students')   EXEC('CREATE SCHEMA students');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'enrollments') EXEC('CREATE SCHEMA enrollments');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'lessons')    EXEC('CREATE SCHEMA lessons');

            IF OBJECT_ID('schools.Schools','U') IS NULL
            BEGIN
                CREATE TABLE schools.Schools (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    Name         NVARCHAR(200)    NOT NULL,
                    Address      NVARCHAR(500)    NOT NULL,
                    ContactEmail NVARCHAR(200)    NOT NULL,
                    IsActive     BIT              NOT NULL,
                    RegisteredAt DATETIME2        NOT NULL,
                    CONSTRAINT PK_Schools PRIMARY KEY (Id));
            END

            IF OBJECT_ID('schools.Instructors','U') IS NULL
            BEGIN
                CREATE TABLE schools.Instructors (
                    Id            UNIQUEIDENTIFIER NOT NULL,
                    SchoolId      UNIQUEIDENTIFIER NOT NULL,
                    FullName      NVARCHAR(200)    NOT NULL,
                    LicenseNumber NVARCHAR(50)     NOT NULL,
                    IsAvailable   BIT              NOT NULL,
                    CONSTRAINT PK_Instructors PRIMARY KEY (Id));
                CREATE INDEX IX_Instructors_SchoolId ON schools.Instructors (SchoolId);
            END

            IF OBJECT_ID('students.Students','U') IS NULL
            BEGIN
                CREATE TABLE students.Students (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    FullName     NVARCHAR(200)    NOT NULL,
                    Email        NVARCHAR(200)    NOT NULL,
                    PhoneNumber  NVARCHAR(30)     NULL,
                    DateOfBirth  DATE             NOT NULL,
                    RegisteredAt DATETIME2        NOT NULL,
                    PasswordHash NVARCHAR(500)    NOT NULL,
                    CONSTRAINT PK_Students PRIMARY KEY (Id));
                CREATE UNIQUE INDEX IX_Students_Email ON students.Students (Email);
            END

            IF OBJECT_ID('students.RefreshTokens','U') IS NULL
            BEGIN
                CREATE TABLE students.RefreshTokens (
                    Id              UNIQUEIDENTIFIER NOT NULL,
                    Token           NVARCHAR(200)    NOT NULL,
                    StudentId       UNIQUEIDENTIFIER NOT NULL,
                    Family          NVARCHAR(50)     NOT NULL,
                    ExpiresAt       DATETIME2        NOT NULL,
                    CreatedAt       DATETIME2        NOT NULL,
                    RevokedAt       DATETIME2        NULL,
                    ReplacedByToken NVARCHAR(200)    NULL,
                    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id));
                CREATE UNIQUE INDEX IX_RefreshTokens_Token ON students.RefreshTokens (Token);
                CREATE INDEX IX_RefreshTokens_StudentId ON students.RefreshTokens (StudentId);
            END

            IF OBJECT_ID('enrollments.Enrollments','U') IS NULL
            BEGIN
                CREATE TABLE enrollments.Enrollments (
                    Id                 UNIQUEIDENTIFIER NOT NULL,
                    StudentId          UNIQUEIDENTIFIER NOT NULL,
                    DrivingSchoolId    UNIQUEIDENTIFIER NOT NULL,
                    InstructorId       UNIQUEIDENTIFIER NULL,
                    Fee                DECIMAL(18,2)    NOT NULL,
                    PaymentStatus      NVARCHAR(MAX)    NOT NULL,
                    Status             NVARCHAR(MAX)    NOT NULL,
                    EnrolledAt         DATETIME2        NOT NULL,
                    PaymentConfirmedAt DATETIME2        NULL,
                    CancelledAt        DATETIME2        NULL,
                    CONSTRAINT PK_Enrollments PRIMARY KEY (Id));
                CREATE INDEX IX_Enrollments_StudentId ON enrollments.Enrollments (StudentId);
                CREATE INDEX IX_Enrollments_StudentId_Status ON enrollments.Enrollments (StudentId, Status);
            END

            IF OBJECT_ID('lessons.Lessons','U') IS NULL
            BEGIN
                CREATE TABLE lessons.Lessons (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    EnrollmentId UNIQUEIDENTIFIER NOT NULL,
                    StudentId    UNIQUEIDENTIFIER NOT NULL,
                    InstructorId UNIQUEIDENTIFIER NOT NULL,
                    ScheduledAt  DATETIME2        NOT NULL,
                    Duration     FLOAT            NOT NULL,
                    Status       NVARCHAR(MAX)    NOT NULL,
                    Notes        NVARCHAR(MAX)    NULL,
                    CompletedAt  DATETIME2        NULL,
                    CONSTRAINT PK_Lessons PRIMARY KEY (Id));
                CREATE INDEX IX_Lessons_StudentId_ScheduledAt ON lessons.Lessons (StudentId, ScheduledAt);
                CREATE INDEX IX_Lessons_EnrollmentId ON lessons.Lessons (EnrollmentId);
            END

            IF OBJECT_ID('enrollments.OutboxMessages','U') IS NULL
            BEGIN
                CREATE TABLE enrollments.OutboxMessages (
                    Id          UNIQUEIDENTIFIER NOT NULL,
                    EventType   NVARCHAR(500)    NOT NULL,
                    Payload     NVARCHAR(MAX)    NOT NULL,
                    CreatedAt   DATETIME2        NOT NULL,
                    ProcessedAt DATETIME2        NULL,
                    Error       NVARCHAR(MAX)    NULL,
                    CONSTRAINT PK_EnrollmentOutboxMessages PRIMARY KEY (Id));
                CREATE INDEX IX_EnrollmentOutbox_Unprocessed ON enrollments.OutboxMessages (ProcessedAt)
                    WHERE ProcessedAt IS NULL;
            END

            IF OBJECT_ID('lessons.OutboxMessages','U') IS NULL
            BEGIN
                CREATE TABLE lessons.OutboxMessages (
                    Id          UNIQUEIDENTIFIER NOT NULL,
                    EventType   NVARCHAR(500)    NOT NULL,
                    Payload     NVARCHAR(MAX)    NOT NULL,
                    CreatedAt   DATETIME2        NOT NULL,
                    ProcessedAt DATETIME2        NULL,
                    Error       NVARCHAR(MAX)    NULL,
                    CONSTRAINT PK_LessonOutboxMessages PRIMARY KEY (Id));
                CREATE INDEX IX_LessonOutbox_Unprocessed ON lessons.OutboxMessages (ProcessedAt)
                    WHERE ProcessedAt IS NULL;
            END
            """;
        cmd.ExecuteNonQuery();
        conn.Close();
    }
    else
    {
        sp.GetRequiredService<EnrollmentsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<StudentsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<SchoolsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<LessonsDbContext>().Database.EnsureCreated();
    }
}
catch (Exception ex)
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogWarning(ex, "Schema init failed on startup — app will start but SQL operations may fail.");
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DriveEase API v1"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

public partial class Program { }
