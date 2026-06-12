using DriveEase.Shared.Domain;

namespace DriveEase.Shared.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;
}
