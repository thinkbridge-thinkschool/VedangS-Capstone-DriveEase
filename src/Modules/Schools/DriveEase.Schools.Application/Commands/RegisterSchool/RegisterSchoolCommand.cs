using MediatR;

namespace DriveEase.Schools.Application.Commands.RegisterSchool;

public sealed record RegisterSchoolCommand(
    string Name,
    string Address,
    string ContactEmail) : IRequest<Guid>;
