using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Core.Domain.Repositories;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Order.Infrastructure.Messaging;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString)
    {
        // fallback para variável de ambiente, útil no docker depois
        connectionString ??= Environment.GetEnvironmentVariable("STRING_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string não configurada. " +
                "Defina POSTGRES_CONNECTION ou passe via AddInfrastructure."
            );
        }


        services.AddDbContext<OrderDbContext>(opt =>
        {
            opt.UseNpgsql(
                connectionString,
                npg => npg.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName)
            );
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

        return services;
    }
}
