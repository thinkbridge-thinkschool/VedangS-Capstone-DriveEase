using DriveEase.Lessons.Domain.Events;
using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DriveEase.Lessons.Infrastructure.Workers;

public sealed class LessonReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<LessonReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendRemindersAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpcomingLessonsQuery>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Find all lessons occurring in ~24 hours (within a 25h window checked hourly)
        var upcomingLessons = await repository.GetScheduledWithin25HoursAsync(cancellationToken);

        foreach (var lesson in upcomingLessons)
        {
            var hoursUntil = (lesson.ScheduledAt - DateTime.UtcNow).TotalHours;
            if (hoursUntil is > 23 and <= 25)
            {
                var reminderEvent = LessonBookedEvent.Create(
                    lesson.LessonId, lesson.StudentId, lesson.InstructorId, lesson.ScheduledAt);
                await eventBus.PublishAsync(reminderEvent, cancellationToken);
                logger.LogInformation("Reminder sent for lesson {LessonId}", lesson.LessonId);
            }
        }
    }
}

public record UpcomingLesson(Guid LessonId, Guid StudentId, Guid InstructorId, DateTime ScheduledAt);

public interface IUpcomingLessonsQuery
{
    Task<IReadOnlyList<UpcomingLesson>> GetScheduledWithin25HoursAsync(CancellationToken cancellationToken = default);
}
