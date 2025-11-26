namespace Order.Api.Services;

public interface ISqlExecutionService
{
    Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, CancellationToken ct);
}
