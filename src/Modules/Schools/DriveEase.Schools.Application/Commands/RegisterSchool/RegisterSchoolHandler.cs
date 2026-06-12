using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Commands.RegisterSchool;

public sealed class RegisterSchoolHandler(IDrivingSchoolRepository repository)
    : IRequestHandler<RegisterSchoolCommand, Guid>
{
    public async Task<Guid> Handle(RegisterSchoolCommand request, CancellationToken cancellationToken)
    {
        var school = DrivingSchool.Register(request.Name, request.Address, request.ContactEmail);
        await repository.AddAsync(school, cancellationToken);
        return school.Id;
    }
}
