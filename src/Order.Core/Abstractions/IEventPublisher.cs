using System.Threading;
using System.Threading.Tasks;
using Order.Core.Events;

namespace Order.Core.Abstractions;
public interface IEventPublisher
{
    Task PublishSameEventNTimesAsync(OrderCreatedEvent createdEvent, int n = 5, CancellationToken ct = default);
}

