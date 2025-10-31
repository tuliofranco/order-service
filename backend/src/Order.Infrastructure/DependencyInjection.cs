using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Core.Application.Abstractions.Messaging.Outbox;
using Order.Core.Application.Abstractions.Repositories;
using Order.Infrastructure.Messaging.Outbox;
using Order.Infrastructure.Persistence;
using Order.Core.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence.Repositories;
using Order.Infrastructure.Messaging.ServiceBus;



namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? dbConnection = null,
        string? asbConnection = null,
        string? asbEntityName = null,
        bool enableOutboxProcessor = true)
    {
        dbConnection ??= Environment.GetEnvironmentVariable("STRING_CONNECTION");
        if (string.IsNullOrWhiteSpace(dbConnection))
            throw new InvalidOperationException("Connection string do Postgres não configurada (defina STRING_CONNECTION ou passe via AddInfrastructure).");

        // Azure Service Bus
        asbConnection ??= Environment.GetEnvironmentVariable("ASB_CONNECTION");
        asbEntityName ??= Environment.GetEnvironmentVariable("ASB_ENTITY");

        // ====== DbContext ======
        services.AddDbContext<OrderDbContext>(opt =>
        {
            opt.UseNpgsql(
                dbConnection,
                npg => npg.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName)
            );
        });

        // ====== Unit of Work ======
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // ====== Repositório ======
        services.AddScoped<IOrderRepository, EfOrderRepository>();

        services.AddScoped<IOrderStatusHistoryRepository, EfOrderStatusHistoryRepository>();

        // ====== Outbox: Serializer, Store e Publisher ======
        services.AddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();

        // ServiceBusClient (singleton)
        if (string.IsNullOrWhiteSpace(asbConnection))
            throw new InvalidOperationException("Azure Service Bus não configurado (defina ASB_CONNECTION ou passe via AddInfrastructure).");

        services.AddSingleton(new ServiceBusClient(asbConnection));

        // Outbox Publisher (usa o ServiceBusClient e a entidade configurada)
        if (string.IsNullOrWhiteSpace(asbEntityName))
            throw new InvalidOperationException("Nome da fila/tópico do ASB não configurado (defina ASB_ENTITY ou passe via AddInfrastructure).");

        services.AddScoped<IOutboxPublisher>(sp =>
            new ServiceBusOutboxPublisher(
                sp.GetRequiredService<ServiceBusClient>(),
                asbEntityName!,
                sp.GetRequiredService<ILogger<ServiceBusOutboxPublisher>>()
            ));

        if (enableOutboxProcessor)
        {
            services.AddHostedService<OutboxProcessor>();
        }

        return services;
    }
}
