using DriveEase.Shared.Domain;

namespace DriveEase.Enrollments.Domain.Events;

public sealed record EnrollmentCancelledEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid EnrollmentId,
    Guid StudentId,
    string Reason) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(EnrollmentCancelledEvent);

    public static EnrollmentCancelledEvent Create(Guid enrollmentId, Guid studentId, string reason) =>
        new(Guid.NewGuid(), DateTime.UtcNow, enrollmentId, studentId, reason);
}
