using DriveEase.Enrollments.Domain.Aggregates;

namespace DriveEase.Enrollments.Domain.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetActiveByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetPendingPaymentOlderThanAsync(TimeSpan age, CancellationToken cancellationToken = default);
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}
