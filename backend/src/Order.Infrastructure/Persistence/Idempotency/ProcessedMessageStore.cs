using Microsoft.EntityFrameworkCore;
using Order.Core.Application.Abstractions.Idempotency;

namespace Order.Infrastructure.Persistence.Idempotency;

public sealed class ProcessedMessageStore(OrderDbContext db) : IProcessedMessageStore
{
    private readonly OrderDbContext _db = db;

    public async Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO processed_messages (message_id, processed_at_utc)
            VALUES ({messageId}, {DateTime.UtcNow})
            ON CONFLICT (message_id) DO NOTHING;
        ", ct);

        return rows == 1;
    }
}
