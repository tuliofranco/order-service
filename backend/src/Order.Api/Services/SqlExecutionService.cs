using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Order.Api.Services;

public class SqlExecutionService : ISqlExecutionService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlExecutionService> _logger;

    private static readonly string[] AllowedTables = { "orders", "order_status_history" };

    public SqlExecutionService(
        IConfiguration configuration,
        ILogger<SqlExecutionService> logger)
    {
        var conn = configuration["STRING_CONNECTION"]
        ?? Environment.GetEnvironmentVariable("STRING_CONNECTION");
        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("STRING_CONNECTION não configurada.");

        _connectionString = conn;


        _logger = logger;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, CancellationToken ct)
    {
        // Só pra ficar mais legível no log
        var normalizedSql = sql.Replace("\n", " ").Trim();

        _logger.LogInformation("Validando SQL recebido: {Sql}", normalizedSql);

        try
        {
            ValidateSql(sql);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha na validação do SQL: {Sql}", normalizedSql);
            throw;
        }

        _logger.LogInformation("Executando SQL no banco: {Sql}", normalizedSql);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(sql, conn)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 15
            };

            using var reader = await cmd.ExecuteReaderAsync(ct);

            var results = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i) ?? $"column_{i}";
                    #pragma warning disable CS8601 // Possível atribuição de referência nula.
                    row[columnName!] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    #pragma warning restore CS8601 // Possível atribuição de referência nula.
                }

                results.Add(row);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "SQL executado com sucesso. Linhas retornadas: {Count}. Tempo: {ElapsedMs}ms",
                results.Count,
                stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao executar SQL no banco. Tempo: {ElapsedMs}ms. SQL: {Sql}",
                stopwatch.ElapsedMilliseconds,
                normalizedSql);

            throw;
        }
    }

    private void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new Exception("SQL vazio.");

        // Remove espaços e um ponto-e-vírgula final, se tiver
        var trimmed = sql.Trim();

        if (trimmed.EndsWith(";"))
            trimmed = trimmed[..^1].Trim(); // remove o ; final

        var cleaned = trimmed.ToLowerInvariant();

        if (!cleaned.StartsWith("select"))
            throw new Exception("Apenas queries SELECT são permitidas.");

        // Agora, se ainda tiver ';', é porque tem outra instrução
        if (cleaned.Contains(";"))
            throw new Exception("Múltiplas instruções SQL não são permitidas.");

        bool containsAllowedTable = AllowedTables.Any(t => cleaned.Contains(t));
        if (!containsAllowedTable)
            throw new Exception("A query deve acessar apenas tabelas permitidas.");

        var forbidden = new[]
        {
            "delete", "update", "insert", "alter", "drop", "truncate",
            "create", "grant", "revoke"
        };

        if (forbidden.Any(f => cleaned.Contains(f)))
            throw new Exception("A query contém comandos proibidos.");

        if (!cleaned.Contains("limit"))
            throw new Exception("A query deve conter um LIMIT.");
    }

}
