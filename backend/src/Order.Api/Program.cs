using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Messaging.ServiceBus;
using Order.Infrastructure.Persistence;
using Order.Core.Domain.Repositories;
using Order.Core.Services;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.HealthChecks;
using Order.Core.Abstractions;
using HealthChecks.UI.Client;
using Order.Core.Events;

var builder = WebApplication.CreateBuilder(args);

try { DotNetEnv.Env.TraversePath().Load(); } catch { }

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
    o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderService, Order.Core.Services.OrderService>();

if (builder.Environment.IsEnvironment("Testing"))
{
    // ---- TESTING: InMemory + HealthCheck básico, sem ServiceBus ----
    builder.Services.AddDbContext<OrderDbContext>(opt =>
        opt.UseInMemoryDatabase("OrdersTestsDb"));

    builder.Services.AddHealthChecks()
        .AddCheck("noop", () => HealthCheckResult.Healthy());

    // Publishers no-op para não depender do Service Bus nos testes
    builder.Services.AddSingleton<IServiceBusPublisher, NoopBusPublisher>();
    builder.Services.AddSingleton<IEventPublisher, NoopEventPublisher>();
}
else
{
    // ---- DEV/PROD: Postgres + ServiceBus + HealthChecks ----
    var connString = builder.Configuration["DEFAULT_CONNECTION"];
    if (string.IsNullOrWhiteSpace(connString))
        throw new InvalidOperationException("Nenhuma connection string encontrada. Defina DEFAULT_CONNECTION no .env.");

    builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(connString));

    builder.Services.AddSingleton(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var sbConn = cfg["SERVICEBUS_CONNECTION"];
        if (string.IsNullOrWhiteSpace(sbConn))
            throw new InvalidOperationException("Service Bus connection string não configurada. Defina SERVICEBUS_CONNECTION no .env.");
        return new ServiceBusClient(sbConn);
    });

    builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
    builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

    builder.Services.AddHealthChecks()
        .AddNpgSql(connString, name: "postgres", failureStatus: HealthStatus.Unhealthy)
        .AddCheck<ServiceBusHealthCheck>("servicebus", failureStatus: HealthStatus.Unhealthy);
}

var app = builder.Build();

// Migra só quando for relacional (evita conflito nos testes InMemory)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (db.Database.IsRelational())
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

// Necessário para WebApplicationFactory nos testes
public partial class Program { }

// ---- no-op publishers (podem ficar neste arquivo ou em arquivos próprios) ----
file sealed class NoopBusPublisher : IServiceBusPublisher
{

    public Task PublishOrderCreatedAsync(Guid orderId, string eventType, Guid correlationId, CancellationToken ct = default)  => Task.CompletedTask;
}
file sealed class NoopEventPublisher : IEventPublisher
{

    public Task PublishAsync(OrderCreatedEvent createdEvent, CancellationToken ct = default) => Task.CompletedTask;
}
