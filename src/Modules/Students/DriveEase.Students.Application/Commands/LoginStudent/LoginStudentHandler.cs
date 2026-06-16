using DriveEase.Students.Domain.Repositories;
using MediatR;

namespace DriveEase.Students.Application.Commands.LoginStudent;

public sealed class LoginStudentHandler(IStudentRepository repository)
    : IRequestHandler<LoginStudentCommand, LoginResultDto?>
{
    public async Task<LoginResultDto?> Handle(LoginStudentCommand request, CancellationToken cancellationToken)
    {
        // TODO: Get student by email
        // TODO: Verify password hash
        // TODO: Return LoginResultDto or null if invalid
        throw new NotImplementedException();
    }
}
