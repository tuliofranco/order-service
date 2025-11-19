#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Worker.Consumers;
using Order.Infrastructure.Persistence;
using Order.Worker;
using Order.Worker.Idempotency;
using OrderService.Infrastructure;
using Order.Worker.Processing;
using Order.Worker.Notification;
using Azure.Messaging.ServiceBus;
using DotNetEnv;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
});

try { Env.TraversePath().Load(); } catch { }


builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

var services = builder.Services;
var configuration = builder.Configuration;
var hubUrl =
    Environment.GetEnvironmentVariable("NEXT_PUBLIC_ORDER_HUB_URL") ?? configuration["Notification:HubUrl"];

if (string.IsNullOrWhiteSpace(hubUrl))
{
    throw new InvalidOperationException(
        "Hub URL do SignalR não configurado. " +
        "Defina NEXT_PUBLIC_ORDER_HUB_URL ou Notification:HubUrl."
    );
}

services.Configure<WorkerNotificationOptions>(options =>
{
    options.HubUrl = hubUrl;
});

services.AddSingleton<IWorkerOrderNotification, WorkerOrderNotification>();

builder.Services.AddScoped<ProcessOrder>();   
builder.Services.AddInfrastructure(enableOutboxProcessor: true);

var queueName = configuration["ASB_ENTITY"]
    ?? throw new InvalidOperationException("Configuração 'ASB_ENTITY' não foi definida.");

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
