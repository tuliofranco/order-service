#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;
using Order.Worker.Services;
using Order.Worker.Consumers;
using Order.Core.Domain.Repositories;
using Order.Infrastructure.Persistence;
using Order.Worker;

// 1. Cria o "builder" do host do worker (.NET 8/9 style)
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 2. Configuração (appsettings.json + variáveis de ambiente)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

// 3. Registrar dependências de infra, domínio e consumidor ------------------

// --- Postgres / EF Core ---
var connString = builder.Configuration["DEFAULT_CONNECTION"];

if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException("Missing Postgres connection string. Define DEFAULT_CONNECTION no .env.");
}

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(connString);
});

// repositório que atualiza pedidos no banco
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();

// --- Azure Service Bus ---
var sbConn = builder.Configuration["SERVICEBUS_CONNECTION"];

if (string.IsNullOrWhiteSpace(sbConn))
{
    throw new InvalidOperationException("Missing Service Bus connection string. Define SERVICEBUS_CONNECTION no .env.");
}


var queueName =
    builder.Configuration["SERVICEBUS_QUEUE"] 
    ?? "orders"; 

// client do Service Bus (1 só para o processo inteiro)
builder.Services.AddSingleton<ServiceBusClient>(_ =>
{
    return new ServiceBusClient(sbConn);
});

// serviço auxiliar que vai aplicar as mudanças de status no pedido
builder.Services.AddScoped<StatusUpdater>();

// background service que consome a fila e processa cada mensagem
builder.Services.AddHostedService(sp =>
{
    var logger = sp.GetRequiredService<ILogger<OrderCreatedConsumer>>();
    var client = sp.GetRequiredService<ServiceBusClient>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

    return new OrderCreatedConsumer(
        logger,
        client,
        scopeFactory,
        queueName
    );
});

// 4. Constrói o host real (o runtime do worker)
using IHost host = builder.Build();

// 5. Roda até matarem o processo (Ctrl+C / container stop)
await host.RunAsync();
