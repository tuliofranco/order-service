using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Persistence;
using Order.Core.Domain.Repositories;
using Order.Core.Services;
using Order.Infrastructure.Messaging.Abstractions;
using Order.Infrastructure.Messaging.AzureServiceBus;
using Azure.Messaging.ServiceBus;
using OrderAppService = Order.Core.Services.OrderService;
using Order.Infrastructure.HealthChecks;
using Order.Core.Abstractions;


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

var connString = builder.Configuration["DEFAULT_CONNECTION"];

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

// registra o publisher
builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();


builder.Services.AddHealthChecks();

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

builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
builder.Services.AddScoped<IOrderService, OrderAppService>();
builder.Services.AddHealthChecks()
    .AddCheck<ServiceBusHealthCheck>("servicebus");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// Swagger sempre ativo (facilita teste)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
