using DriveEase.Shared.Domain;

namespace DriveEase.Enrollments.Domain.Events;

public sealed record PaymentFailedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid EnrollmentId,
    Guid StudentId,
    string Reason) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(PaymentFailedEvent);

    public static PaymentFailedEvent Create(Guid enrollmentId, Guid studentId, string reason) =>
        new(Guid.NewGuid(), DateTime.UtcNow, enrollmentId, studentId, reason);
}
