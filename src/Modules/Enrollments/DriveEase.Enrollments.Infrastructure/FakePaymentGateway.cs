using DriveEase.Enrollments.Application.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace DriveEase.Enrollments.Infrastructure;

public sealed class FakePaymentGateway(ILogger<FakePaymentGateway> logger) : IPaymentGateway
{
    // Retry 3×, circuit breaks after 50% failures over 30s, total timeout 5s.
    // Same pattern as the Quotes API's Polly setup (Day 22).
    private static readonly ResiliencePipeline<bool> _pipeline =
        new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = 3,
                Delay            = TimeSpan.FromMilliseconds(200),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = args => ValueTask.FromResult(args.Outcome.Exception is not null)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                SamplingDuration  = TimeSpan.FromSeconds(30),
                FailureRatio      = 0.5,
                MinimumThroughput = 5,
                BreakDuration     = TimeSpan.FromSeconds(15)
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

    public Task<bool> ChargeAsync(Guid studentId, decimal amount, CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(async ct =>
        {
            await Task.Delay(50, ct);
            logger.LogInformation(
                "FakePaymentGateway: charged student {StudentId} £{Amount:F2}",
                studentId, amount);
            return true;
        }, cancellationToken).AsTask();
}
