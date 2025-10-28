using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Messaging.ServiceBus;

using Order.Infrastructure.Persistence;
using Order.Core.Domain.Repositories;
using Order.Core.Services;
using Order.Infrastructure.Messaging.Abstractions;
using Order.Infrastructure.Messaging.AzureServiceBus;
using Order.Infrastructure.HealthChecks;
using Order.Core.Abstractions;
using OrderAppService = Order.Core.Services.OrderService;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connString =
    builder.Configuration["DEFAULT_CONNECTION"]
    ?? "Host=db;Port=5432;Database=orders_db;Username=postgres;Password=postgres";

if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException(
        "Nenhuma connection string encontrada. Defina DEFAULT_CONNECTION no .env."
    );
}

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(connString);
});

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serviceBusConnectionString = config["SERVICEBUS_CONNECTION"];

    if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
    {
        throw new InvalidOperationException(
            "Service Bus connection string não configurada. Defina SERVICEBUS_CONNECTION no .env."
        );
    }

    return new ServiceBusClient(serviceBusConnectionString);
});

builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderAppService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --- Health Checks (tudo em um único encadeamento) ---
builder.Services
    .AddHealthChecks()
    .AddNpgSql(connString, name: "postgres", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<ServiceBusHealthCheck>(
        name: "servicebus",
        failureStatus: HealthStatus.Unhealthy
    );

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
