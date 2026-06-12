using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Schools.Infrastructure.Persistence;

public sealed class SchoolRepository(SchoolsDbContext dbContext) : IDrivingSchoolRepository
{
    public Task<DrivingSchool?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Schools.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DrivingSchool>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Schools.Where(s => s.IsActive).ToListAsync(cancellationToken);

    public async Task AddAsync(DrivingSchool school, CancellationToken cancellationToken = default)
    {
        await dbContext.Schools.AddAsync(school, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class InstructorRepository(SchoolsDbContext dbContext) : IInstructorRepository
{
    public Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Instructors.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Instructor>> GetAvailableBySchoolAsync(
        Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Instructors
            .Where(i => i.SchoolId == schoolId && i.IsAvailable)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        await dbContext.Instructors.AddAsync(instructor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        dbContext.Instructors.Update(instructor);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
