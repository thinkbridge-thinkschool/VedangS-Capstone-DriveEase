using DriveEase.Enrollments.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnPaymentFailed(INotificationSender sender)
    : IIntegrationEventHandler<PaymentFailedEvent>
{
    public async Task HandleAsync(PaymentFailedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Action Required — Payment Failed",
            body: $"Your payment for enrollment {evt.EnrollmentId} could not be processed. " +
                  $"Reason: {evt.Reason}. Please retry within 72 hours or your enrollment will be cancelled.",
            cancellationToken);
    }
}
