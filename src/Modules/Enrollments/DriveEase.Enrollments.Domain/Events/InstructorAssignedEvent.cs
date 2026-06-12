using DriveEase.Shared.Domain;

namespace DriveEase.Enrollments.Domain.Events;

public sealed record InstructorAssignedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid EnrollmentId,
    Guid StudentId,
    Guid InstructorId) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(InstructorAssignedEvent);

    public static InstructorAssignedEvent Create(Guid enrollmentId, Guid studentId, Guid instructorId) =>
        new(Guid.NewGuid(), DateTime.UtcNow, enrollmentId, studentId, instructorId);
}
