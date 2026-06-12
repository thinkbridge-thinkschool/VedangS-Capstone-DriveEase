using MediatR;

namespace DriveEase.Schools.Application.Queries.GetSchool;

public sealed record SchoolDto(Guid Id, string Name, string Address, string ContactEmail, bool IsActive);

public sealed record GetSchoolQuery(Guid SchoolId) : IRequest<SchoolDto?>;
