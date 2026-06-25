using MediatR;

namespace DriveEase.Schools.Application.Queries.GetAllSchools;

public sealed record SchoolSummaryDto(Guid Id, string Name, string Address, string ContactEmail);

public sealed record GetAllSchoolsQuery : IRequest<IReadOnlyList<SchoolSummaryDto>>;
