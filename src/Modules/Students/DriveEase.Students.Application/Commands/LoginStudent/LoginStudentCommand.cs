using MediatR;

namespace DriveEase.Students.Application.Commands.LoginStudent;

public sealed record LoginStudentCommand(
    string Email,
    string Password) : IRequest<LoginResultDto?>;

public sealed record LoginResultDto(Guid StudentId, string FullName, string Email);
