using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Queries.GetSchool;

public sealed class GetSchoolHandler(IDrivingSchoolRepository repository)
    : IRequestHandler<GetSchoolQuery, SchoolDto?>
{
    public async Task<SchoolDto?> Handle(GetSchoolQuery request, CancellationToken cancellationToken)
    {
        var school = await repository.GetByIdAsync(request.SchoolId, cancellationToken);
        if (school is null) return null;

        return new SchoolDto(school.Id, school.Name, school.Address, school.ContactEmail, school.IsActive);
    }
}
