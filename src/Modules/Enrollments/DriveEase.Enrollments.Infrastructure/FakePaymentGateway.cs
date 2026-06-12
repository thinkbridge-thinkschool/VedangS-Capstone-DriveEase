using DriveEase.Enrollments.Application.Services;
using Microsoft.Extensions.Logging;

namespace DriveEase.Enrollments.Infrastructure;

public sealed class FakePaymentGateway(ILogger<FakePaymentGateway> logger) : IPaymentGateway
{
    public async Task<bool> ChargeAsync(Guid studentId, decimal amount, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        logger.LogInformation("FakePaymentGateway: charging student {StudentId} amount {Amount:C}", studentId, amount);

        // Always succeeds in dev; swap with real gateway in production
        return true;
    }
}
