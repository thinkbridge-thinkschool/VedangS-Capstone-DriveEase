using DriveEase.Students.Domain.Entities;
using DriveEase.Students.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class StudentRepository(StudentsDbContext dbContext) : IStudentRepository
{
    public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await dbContext.Students.AddAsync(student, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        dbContext.Students.Update(student);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
