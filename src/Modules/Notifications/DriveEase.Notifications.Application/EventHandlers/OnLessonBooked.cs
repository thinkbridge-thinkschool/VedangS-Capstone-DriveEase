using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnLessonBooked(INotificationSender sender)
    : IIntegrationEventHandler<LessonBookedEvent>
{
    public async Task HandleAsync(LessonBookedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Lesson Booked",
            body: $"Your driving lesson is confirmed for {evt.ScheduledAt:f} (UTC). " +
                  "You'll receive a reminder 24 hours before.",
            cancellationToken);
    }
}
