using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetLesson;

public sealed record LessonDto(
    Guid Id,
    Guid EnrollmentId,
    Guid StudentId,
    Guid InstructorId,
    DateTime ScheduledAt,
    string Status,
    string? Notes,
    DateTime? CompletedAt);

public sealed record GetLessonQuery(Guid LessonId) : IRequest<LessonDto?>;
