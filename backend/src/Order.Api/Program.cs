using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Infrastructure.Persistence;
using Order.Core.Domain.Repositories;
using Order.Core.Services;
using Order.Infrastructure.HealthChecks;
using Order.Core.Abstractions;
using HealthChecks.UI.Client;
using Order.Api.Infrastructure.Logging;
using Order.Core.Events;
using Order.Api.Health;
using OrderService.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

try { DotNetEnv.Env.TraversePath().Load(); } catch { }


builder.Services.AddSingleton<IHealthCheckPublisher, ComponentHealthPublisher>();

builder.Services.Configure<HealthCheckPublisherOptions>(opt =>
{
    opt.Delay = TimeSpan.Zero;
    opt.Period = TimeSpan.FromMinutes(1);
    opt.Predicate = _ => true;
});

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
    o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IOrderService, Order.Core.Services.OrderService>();


builder.Services.AddInfrastructure(enableOutboxProcessor: false);
builder.Services.AddSingleton<IHealthCheck, ServiceBusQueueHealthCheck>();
builder.Services.AddSingleton<IHealthCheck, PostgresDbHealthCheck>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API alive"))
    .AddCheck<ServiceBusQueueHealthCheck>("servicebus")
    .AddCheck<PostgresDbHealthCheck>("postgres");

builder.Services.AddSingleton<IHealthCheckPublisher, ComponentHealthPublisher>();

builder.Services.Configure<HealthCheckPublisherOptions>(opt =>
{
    opt.Delay = TimeSpan.Zero;
    opt.Period = TimeSpan.FromMinutes(1);
    opt.Predicate = _ => true;
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.UseRouting();
app.UseMiddleware<OrderIdRouteScopeMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("default");
app.MapControllers();



app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }

