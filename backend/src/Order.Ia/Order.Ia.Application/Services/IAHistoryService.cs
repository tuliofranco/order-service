using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Order.Ia.Application.Entities;

namespace Order.Ia.Application.Services;

public class IAHistoryService
{
    private readonly IMongoCollection<IaHistory> _collection;

    public IAHistoryService(IConfiguration config)
    {
        var conn = config["MONGO_CONNECTION"]
            ?? throw new Exception("MONGO_CONNECTION missing");

        var dbName = config["MONGO_DB"]
            ?? throw new Exception("MONGO_DB missing");

        var client = new MongoClient(conn);
        var db = client.GetDatabase(dbName);

        _collection = db.GetCollection<IaHistory>("history");
    }

    public async Task SaveAsync(string question,IaResponse response)
    {
        var entry = new IaHistory
        {
            Question = question,
            Answer = response.Answer,
            ModelUsed = response.Model,
            TokensUsed = response.TokensUsed
        };

        await _collection.InsertOneAsync(entry);
    }

    public async Task<List<IaHistory>> ListAsync()
    {
        return await _collection.Find(_ => true)
                                .SortByDescending(x => x.CreatedAt)
                                .Limit(50)
                                .ToListAsync();
    }
}
