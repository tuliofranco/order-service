using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Persistence;
using Order.Core.Domain.Repositories;
using Order.Core.Services;

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
    builder.Configuration.GetValue<string>("DEFAULT_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException(
        "Nenhuma connection string encontrada. Defina DEFAULT_CONNECTION no .env " +
        "ou configure ConnectionStrings:DefaultConnection no appsettings.json."
    );
}

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(connString);
});

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

builder.Services.AddScoped<IOrderService, Order.Infrastructure.Services.OrderService>();

var app = builder.Build();

// ---------------------------------------------------------
// Middleware / Pipeline HTTP
// ---------------------------------------------------------

// Swagger sempre ativo (facilita teste)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

// Em dev local pode manter HTTPS redirect, em Docker a gente provavelmente tira
app.UseHttpsRedirection();

// Controllers REST
app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health");

app.Run();
