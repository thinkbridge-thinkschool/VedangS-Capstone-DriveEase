using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnLessonCompleted(INotificationSender sender)
    : IIntegrationEventHandler<LessonCompletedEvent>
{
    public async Task HandleAsync(LessonCompletedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Lesson Completed — Share Your Feedback",
            body: $"Your lesson on {evt.OccurredAt:f} is marked complete. " +
                  "We'd love your feedback! Please rate your experience.",
            cancellationToken);
    }
}
