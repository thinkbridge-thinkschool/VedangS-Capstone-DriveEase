using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Queries.GetAllSchools;

public sealed class GetAllSchoolsHandler(IDrivingSchoolRepository repository)
    : IRequestHandler<GetAllSchoolsQuery, IReadOnlyList<SchoolSummaryDto>>
{
    public async Task<IReadOnlyList<SchoolSummaryDto>> Handle(GetAllSchoolsQuery request, CancellationToken cancellationToken)
    {
        var schools = await repository.GetAllActiveAsync(cancellationToken);
        return schools.Select(s => new SchoolSummaryDto(s.Id, s.Name, s.Address, s.ContactEmail)).ToList();
    }
}
