using DriveEase.Shared.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Shared.Messaging;

public sealed class InMemoryEventBus(IServiceProvider serviceProvider) : IEventBus
{
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<T>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(integrationEvent, cancellationToken);
    }
}

public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T integrationEvent, CancellationToken cancellationToken = default);
}
