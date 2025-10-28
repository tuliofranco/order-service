using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Idempotency;

public sealed class ProcessedMessageStore : IProcessedMessageStore
{
    private readonly OrderDbContext _db;

    public ProcessedMessageStore(OrderDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasProcessedAsync(string messageId, CancellationToken ct)
        => _db.ProcessedMessages.AnyAsync(x => x.MessageId == messageId, ct);

    public async Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct)
    {
        var rows = await _db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO processed_messages (message_id, processed_at_utc)
            VALUES ({0}, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (message_id) DO NOTHING;
        ", new object[] { messageId }, ct);

        return rows == 1;
    }
}
