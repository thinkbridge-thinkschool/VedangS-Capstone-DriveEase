using DriveEase.Shared.Domain;

namespace DriveEase.Lessons.Domain.Events;

public sealed record LessonCompletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid LessonId,
    Guid EnrollmentId,
    Guid StudentId,
    Guid InstructorId) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(LessonCompletedEvent);

    public static LessonCompletedEvent Create(Guid lessonId, Guid enrollmentId, Guid studentId, Guid instructorId) =>
        new(Guid.NewGuid(), DateTime.UtcNow, lessonId, enrollmentId, studentId, instructorId);
}
