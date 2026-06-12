using DriveEase.Enrollments.Domain.Events;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnInstructorAssigned(INotificationSender sender)
    : IIntegrationEventHandler<InstructorAssignedEvent>
{
    public async Task HandleAsync(InstructorAssignedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Instructor Assigned",
            body: $"Great news! An instructor has been assigned to your enrollment {evt.EnrollmentId}. " +
                  "You can now book your first lesson.",
            cancellationToken);
    }
}
