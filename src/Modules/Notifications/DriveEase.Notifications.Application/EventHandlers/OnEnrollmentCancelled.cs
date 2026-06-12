using DriveEase.Enrollments.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnEnrollmentCancelled(INotificationSender sender)
    : IIntegrationEventHandler<EnrollmentCancelledEvent>
{
    public async Task HandleAsync(EnrollmentCancelledEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Enrollment Cancelled",
            body: $"Your enrollment {evt.EnrollmentId} has been cancelled. Reason: {evt.Reason}.",
            cancellationToken);
    }
}
