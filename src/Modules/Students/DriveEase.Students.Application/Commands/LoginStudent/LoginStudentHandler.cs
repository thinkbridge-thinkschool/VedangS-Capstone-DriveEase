using DriveEase.Students.Domain.Repositories;
using MediatR;

namespace DriveEase.Students.Application.Commands.LoginStudent;

public sealed class LoginStudentHandler(
    IStudentRepository repository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<LoginStudentCommand, LoginResultDto?>
{
    public async Task<LoginResultDto?> Handle(LoginStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await repository.GetByEmailAsync(request.Email, cancellationToken);
        if (student is null)
            return null;

        if (!passwordHasher.Verify(request.Password, student.PasswordHash))
            return null;

        return new LoginResultDto(student.Id, student.FullName, student.Email);
    }
}
