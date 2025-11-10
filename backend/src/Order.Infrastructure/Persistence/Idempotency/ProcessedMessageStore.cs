using Microsoft.EntityFrameworkCore;
using Order.Core.Application.Abstractions.Idempotency;

namespace Order.Infrastructure.Persistence.Idempotency;

public sealed class ProcessedMessageStore(OrderDbContext db) : IProcessedMessageStore
{
    public Task<bool> HasProcessedAsync(string messageId, CancellationToken ct = default)
        => db.ProcessedMessages.AnyAsync(x => x.MessageId == messageId, ct);

    public async Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO processed_messages (message_id, processed_at_utc)
            VALUES ({messageId}, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (message_id) DO NOTHING;", ct);

        return rows == 1;
    }
}
