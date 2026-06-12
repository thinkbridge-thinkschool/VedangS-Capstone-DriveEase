namespace DriveEase.Enrollments.Application.Services;

public interface IPaymentGateway
{
    Task<bool> ChargeAsync(Guid studentId, decimal amount, CancellationToken cancellationToken = default);
}
