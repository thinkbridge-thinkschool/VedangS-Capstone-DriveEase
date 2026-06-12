using DriveEase.Students.Domain.Entities;
using DriveEase.Students.Domain.Repositories;
using MediatR;

namespace DriveEase.Students.Application.Commands.RegisterStudent;

public sealed class RegisterStudentHandler(IStudentRepository repository)
    : IRequestHandler<RegisterStudentCommand, Guid>
{
    public async Task<Guid> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Student with email '{request.Email}' already exists.");

        var student = Student.Register(request.FullName, request.Email, request.PhoneNumber, request.DateOfBirth);
        await repository.AddAsync(student, cancellationToken);
        return student.Id;
    }
}
