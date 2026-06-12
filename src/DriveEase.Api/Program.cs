using DriveEase.Enrollments.Infrastructure;
using DriveEase.Lessons.Infrastructure;
using DriveEase.Notifications.Infrastructure;
using DriveEase.Schools.Infrastructure;
using DriveEase.Shared.Messaging;
using DriveEase.Students.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

// Module registrations
var connectionString = builder.Configuration.GetConnectionString("DriveEase") ?? "Data Source=driveease.db";

builder.Services
    .AddEnrollmentsModule(connectionString)
    .AddSchoolsModule(connectionString)
    .AddStudentsModule(connectionString)
    .AddLessonsModule(connectionString)
    .AddNotificationsModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
