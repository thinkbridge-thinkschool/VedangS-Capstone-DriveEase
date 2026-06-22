using DriveEase.Schools.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace DriveEase.Schools.Application.Queries.GetAllSchools;

public sealed class GetAllSchoolsHandler(
    IDrivingSchoolRepository repository,
    HybridCache cache)
    : IRequestHandler<GetAllSchoolsQuery, IReadOnlyList<SchoolSummaryDto>>
{
    // Cache key is stable — list changes only when a school is registered or deactivated.
    private const string CacheKey = "schools:all-active";

    public async Task<IReadOnlyList<SchoolSummaryDto>> Handle(
        GetAllSchoolsQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            CacheKey,
            async ct =>
            {
                var schools = await repository.GetAllActiveAsync(ct);
                return (IReadOnlyList<SchoolSummaryDto>)schools
                    .Select(s => new SchoolSummaryDto(s.Id, s.Name, s.Address, s.ContactEmail))
                    .ToList();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);
}
