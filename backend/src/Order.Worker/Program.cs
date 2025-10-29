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
using Order.Infrastructure.Idempotency;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
});

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

var connString = builder.Configuration["DEFAULT_CONNECTION"];

if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException("Missing Postgres connection string. Define DEFAULT_CONNECTION no .env.");
}

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(connString);
});

builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<ProcessedMessageStore, ProcessedMessageStore>();
var sbConn = builder.Configuration["SERVICEBUS_CONNECTION"];

if (string.IsNullOrWhiteSpace(sbConn))
{
    throw new InvalidOperationException("Missing Service Bus connection string. Define SERVICEBUS_CONNECTION no .env.");
}


var queueName =
    builder.Configuration["SERVICEBUS_QUEUE"] 
    ?? "orders"; 

builder.Services.AddSingleton<ServiceBusClient>(_ =>
{
    return new ServiceBusClient(sbConn);
});

builder.Services.AddScoped<StatusUpdater>();
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


using IHost host = builder.Build();


await host.RunAsync();
