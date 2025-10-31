using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.HealthChecks
{
    public sealed class PostgresDbHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PostgresDbHealthCheck> _logger;

        public PostgresDbHealthCheck(
            IServiceScopeFactory scopeFactory,
            ILogger<PostgresDbHealthCheck> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                var canConnect = await db.Database.CanConnectAsync(cancellationToken);

                if (!canConnect)
                {
                    _logger.LogError("Postgres não respondeu ao CanConnectAsync()");
                    return HealthCheckResult.Unhealthy("Postgres not reachable (CanConnectAsync=false)");
                }
                var _ = await db.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                _logger.LogInformation("Postgres OK");
                return HealthCheckResult.Healthy("Postgres reachable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao checar Postgres");
                return HealthCheckResult.Unhealthy(
                    description: "Postgres unreachable",
                    exception: ex
                );
            }
        }
    }
}
