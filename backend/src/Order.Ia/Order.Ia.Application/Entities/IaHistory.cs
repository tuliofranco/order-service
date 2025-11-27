using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Order.Ia.Application.Entities;

public class IaHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Question { get; set; } = default!;
    public string Answer { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ModelUsed { get; set; }
    public int TokensUsed { get; set; }
}
