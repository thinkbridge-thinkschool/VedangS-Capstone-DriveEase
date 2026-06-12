using DriveEase.Enrollments.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnEnrollmentConfirmed(INotificationSender sender)
    : IIntegrationEventHandler<EnrollmentConfirmedEvent>
{
    public async Task HandleAsync(EnrollmentConfirmedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Enrollment Confirmed — Welcome to DriveEase!",
            body: $"Your enrollment (ID: {evt.EnrollmentId}) has been confirmed. Fee paid: {evt.Fee:C}. " +
                  "A school admin will assign your instructor shortly.",
            cancellationToken);
    }
}
