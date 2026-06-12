using MediatR;

namespace DriveEase.Students.Application.Commands.RegisterStudent;

public sealed record RegisterStudentCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth) : IRequest<Guid>;
