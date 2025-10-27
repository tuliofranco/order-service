using System.Threading;
using System.Threading.Tasks;
using Order.Core.Events;

namespace Order.Core.Abstractions;
public interface IEventPublisher
{
    Task PublishAsync(OrderCreatedEvent createdEvent, CancellationToken ct = default);
}

