#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Worker.Services;
using Order.Worker.Consumers;
using Order.Infrastructure.Persistence;
using Order.Worker;
using Order.Infrastructure.Idempotency;
using OrderService.Infrastructure;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
});

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddScoped<ProcessedMessageStore, ProcessedMessageStore>();

builder.Services.AddInfrastructure(enableOutboxProcessor: true);

var queueName = builder.Configuration["ASB_ENTITY"];

builder.Services.AddScoped<StatusUpdater>();
builder.Services.AddHostedService(sp =>
{
    var logger = sp.GetRequiredService<ILogger<OrderCreatedConsumer>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

    return new OrderCreatedConsumer(
        logger,
        scopeFactory,
        queueName
    );
});

using IHost host = builder.Build();

await host.RunAsync();
