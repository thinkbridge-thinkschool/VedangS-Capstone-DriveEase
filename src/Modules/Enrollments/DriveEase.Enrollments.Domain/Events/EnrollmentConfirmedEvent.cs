using DriveEase.Shared.Domain;

namespace DriveEase.Enrollments.Domain.Events;

public sealed record EnrollmentConfirmedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid EnrollmentId,
    Guid StudentId,
    Guid DrivingSchoolId,
    decimal Fee) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(EnrollmentConfirmedEvent);

    public static EnrollmentConfirmedEvent Create(Guid enrollmentId, Guid studentId, Guid schoolId, decimal fee) =>
        new(Guid.NewGuid(), DateTime.UtcNow, enrollmentId, studentId, schoolId, fee);
}
