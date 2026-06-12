using DriveEase.Students.Domain.Entities;

namespace DriveEase.Students.Domain.Repositories;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
}
