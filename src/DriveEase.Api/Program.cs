using DriveEase.Enrollments.Infrastructure;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Notifications.Infrastructure;
using DriveEase.Schools.Infrastructure;
using DriveEase.Schools.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using DriveEase.Students.Infrastructure;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Prevent a crashing background worker from taking down the whole host
builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Shared event bus — in-memory (swap for Azure Service Bus / RabbitMQ in production)
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Register MediatR handlers from all application assemblies
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
});

// Each module gets its own SQLite file — EnsureCreated only initialises a file once,
// so sharing one file across contexts would leave later contexts with missing tables.
static string DbPath(string module) => $"Data Source=/tmp/driveease-{module}.db";

builder.Services
    .AddEnrollmentsModule(DbPath("enrollments"))
    .AddSchoolsModule(DbPath("schools"))
    .AddStudentsModule(DbPath("students"))
    .AddLessonsModule(DbPath("lessons"))
    .AddNotificationsModule();

var app = builder.Build();

// Ensure SQLite schema exists on every cold start (tables are created if missing)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    sp.GetRequiredService<EnrollmentsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<StudentsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<SchoolsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<LessonsDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
