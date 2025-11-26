using Microsoft.AspNetCore.Mvc;
using Order.Api.Services;

[ApiController]
[Route("internal/sql")]
public class SqlController : ControllerBase
{
    private readonly ISqlExecutionService _sql;
    private readonly ILogger<SqlController> _logger;

    public SqlController(ISqlExecutionService sql, ILogger<SqlController> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Execute([FromBody] SqlRequest request)
    {
        _logger.LogInformation("Executando SQL via /internal/sql: {Query}", request.Query);
         try
        {
            var result = await _sql.ExecuteQueryAsync(request.Query, HttpContext.RequestAborted);

            _logger.LogInformation("Execução SQL concluída com sucesso.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar SQL: {Query}", request.Query);
            return StatusCode(500, new { error = "Erro ao executar SQL.", details = ex.Message });
        }
    }
}

public record SqlRequest(string Query);
