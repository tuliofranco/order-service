using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Core.Abstractions.Messaging.Outbox;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Entities;
using System.Linq;

namespace Order.Infrastructure.Messaging.Outbox;

public class EfOutboxStore : IOutboxStore
{
    private readonly OrderDbContext _db;
    private readonly IEventSerializer _serializer;
    private ILogger<EfOutboxStore> _logger;

    public EfOutboxStore(OrderDbContext db, IEventSerializer serializer, ILogger<EfOutboxStore> logger)
    {
        _db = db;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task AppendAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        var payload = _serializer.Serialize(@event);
        _logger.LogInformation("EfOutboxStore.AppendAsync called: {Type}", @event.Type);


        var entity = new OutboxMessage
        {
            Id = @event.Id,
            Type = @event.Type,
            Payload = payload,
            OccurredOnUtc = @event.OccurredOnUtc
        };

        await _db.OutboxMessages.AddAsync(entity, ct);
    }
    public async Task<IReadOnlyList<OutboxRecord>> FetchPendingBatchAsync(
        int maxBatchSize,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Outbox FETCH start (max={Max})", maxBatchSize);
        var rows = await _db.OutboxMessages
            .AsNoTracking()
            .OrderBy(x => x.OccurredOnUtc)
            .Take(maxBatchSize)
            .ToListAsync(ct);
        _logger.LogInformation("Outbox FETCH rows={Count}", rows.Count);

        var result = rows
            .Select(x => new OutboxRecord(
                x.Id,
                x.Type,
                x.Payload,
                x.OccurredOnUtc
            ))
            .ToList();

        return result;
    }

    public async Task MarkPublishedAsync(Guid outboxId, CancellationToken ct = default)
    {
        await _db.OutboxMessages
            .Where(x => x.Id == outboxId)
            .ExecuteDeleteAsync(ct);
    }
    public Task MarkFailedAsync(Guid outboxId, string error, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

}
