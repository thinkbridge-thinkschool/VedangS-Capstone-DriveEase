using DriveEase.Students.Domain.Repositories;
using MediatR;

namespace DriveEase.Students.Application.Queries.GetStudent;

public sealed class GetStudentHandler(IStudentRepository repository)
    : IRequestHandler<GetStudentQuery, StudentDto?>
{
    public async Task<StudentDto?> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        var student = await repository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null) return null;

        return new StudentDto(student.Id, student.FullName, student.Email, student.PhoneNumber, student.DateOfBirth);
    }
}
