using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Infrastructure.Persistence;
using Order.Core.Application.Services;
using Order.Infrastructure.HealthChecks;
using HealthChecks.UI.Client;
using Order.Api.Infrastructure.Logging;
using Order.Api.Observability.Health;
using OrderService.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Order.Api.Notification;
using System.Text.Json;
using Microsoft.CodeAnalysis.Options;
using System.Text.Json.Serialization;

try { DotNetEnv.Env.TraversePath().Load(); } catch { }

var builder = WebApplication.CreateBuilder(args);


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

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
    o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddScoped<IOrderService, Order.Core.Application.Services.OrderService>();

builder.Services.AddInfrastructure(enableOutboxProcessor: false);

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API alive"))
    .AddCheck<ServiceBusQueueHealthCheck>("servicebus")
    .AddCheck<PostgresDbHealthCheck>("postgres");


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
app.MapHub<OrderNotificationHub>("/hub/notification"); // Apenas o caminho do hub /hub/notification


app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }

