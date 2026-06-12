using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed record BookLessonCommand(
    Guid EnrollmentId,
    Guid StudentId,
    Guid InstructorId,
    DateTime ScheduledAt,
    TimeSpan Duration) : IRequest<Guid>;
