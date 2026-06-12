using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Domain.Events;
using DriveEase.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DriveEase.Enrollments.Infrastructure.Workers;

public sealed class IncompleteEnrollmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IncompleteEnrollmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessStaleEnrollmentsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessStaleEnrollmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Auto-cancel enrollments with failed payment after 72 hours
        var stale = await repository.GetPendingPaymentOlderThanAsync(TimeSpan.FromHours(72), cancellationToken);

        foreach (var enrollment in stale)
        {
            try
            {
                enrollment.Cancel("Auto-cancelled: payment not received within 72 hours.");
                await repository.UpdateAsync(enrollment, cancellationToken);

                var cancelledEvent = (EnrollmentCancelledEvent)enrollment.DomainEvents
                    .First(e => e is EnrollmentCancelledEvent);
                await eventBus.PublishAsync(cancelledEvent, cancellationToken);

                enrollment.ClearDomainEvents();

                logger.LogInformation("Auto-cancelled stale enrollment {EnrollmentId}", enrollment.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-cancel enrollment {EnrollmentId}", enrollment.Id);
            }
        }
    }
}
