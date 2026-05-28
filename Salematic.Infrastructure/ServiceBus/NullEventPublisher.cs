using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.ServiceBus;

public class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync(object evento) => Task.CompletedTask;
}
