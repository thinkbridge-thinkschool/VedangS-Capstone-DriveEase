using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetLesson;

public sealed class GetLessonHandler(ILessonRepository repository)
    : IRequestHandler<GetLessonQuery, LessonDto?>
{
    public async Task<LessonDto?> Handle(GetLessonQuery request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken);
        if (lesson is null) return null;

        return new LessonDto(
            lesson.Id, lesson.EnrollmentId, lesson.StudentId, lesson.InstructorId,
            lesson.ScheduledAt, lesson.Status.ToString(), lesson.Notes, lesson.CompletedAt);
    }
}
