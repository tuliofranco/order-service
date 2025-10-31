using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Order.Api.Observability.Health;

public sealed class ComponentHealthPublisher(ILogger<ComponentHealthPublisher> logger) : IHealthCheckPublisher
{
    private readonly ILogger<ComponentHealthPublisher> _logger = logger;

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        // Scope para herdar component=API em todos os logs daqui
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = "API"
        }))
        {
            _logger.LogInformation(
                "Health report generated: status={Status} totalDurationMs={ElapsedMilliseconds}",
                report.Status.ToString(),
                report.TotalDuration.TotalMilliseconds
            );

            foreach (var (name, entry) in report.Entries)
            {
                _logger.LogInformation(
                    "HealthCheck entry: name={Name} status={Status} durationMs={Duration} description={Description}",
                    name,
                    entry.Status.ToString(),
                    entry.Duration.TotalMilliseconds,
                    entry.Description
                );
            }
        }

        return Task.CompletedTask;
    }
}
