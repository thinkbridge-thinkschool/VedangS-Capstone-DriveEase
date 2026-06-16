using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetStudentLessons;

public sealed class GetStudentLessonsHandler(ILessonRepository repository)
    : IRequestHandler<GetStudentLessonsQuery, IReadOnlyList<StudentLessonDto>>
{
    public async Task<IReadOnlyList<StudentLessonDto>> Handle(GetStudentLessonsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Fetch all lessons for the given StudentId and map to StudentLessonDto
        throw new NotImplementedException();
    }
}
