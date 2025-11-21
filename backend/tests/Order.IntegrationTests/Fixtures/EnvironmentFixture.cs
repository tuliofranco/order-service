using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Networks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Order.Infrastructure.Persistence;
using Xunit;
using DotNetEnv;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;



namespace Order.IntegrationTests.Fixtures;

public sealed class EnvironmentFixture : IAsyncLifetime
{
    private readonly INetwork _network;
    public PostgreSqlContainer Postgres { get; }
    public IContainer Worker { get; }
    public IContainer Api { get; }
    public string DbConnectionForTests { get; private set; } = string.Empty;
    public string DbConnectionForContainers { get; }

    public EnvironmentFixture()
    {
        var envFile = Environment.GetEnvironmentVariable("ENV_FILE") ?? ".env.test";

        try
        {
            Env.TraversePath().Load(envFile);
            Console.WriteLine($"[EnvFixture] .env carregado: {envFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnvFixture] Falha ao carregar {envFile}: {ex.Message}");
        }
        var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "orders_db_tests";
        var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
        var connStringFromEnv = Environment.GetEnvironmentVariable("STRING_CONNECTION");
        var aspEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Test";

        var asbConn = Environment.GetEnvironmentVariable("ASB_CONNECTION");
        var asbEntity = Environment.GetEnvironmentVariable("ASB_ENTITY");
        var ordeHubUrl = Environment.GetEnvironmentVariable("ORDER_HUB_URL");
        _network = new NetworkBuilder()
            .WithName($"orders-test-network-{Guid.NewGuid()}") 
            .Build();

        Postgres = new PostgreSqlBuilder()
            .WithDatabase(dbName)
            .WithUsername(dbUser)
            .WithPassword(dbPass)
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .Build();

        DbConnectionForContainers =
            connStringFromEnv
            ?? $"Host=db;Port=5432;Database={dbName};Username={dbUser};Password={dbPass}";

        Worker = new ContainerBuilder()
            .WithName($"orders-worker-tests-{Guid.NewGuid()}")
            .WithImage("order-worker:tests")
            .WithNetwork(_network)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspEnv)
            .WithEnvironment("STRING_CONNECTION", DbConnectionForContainers) 
            .WithEnvironment("ASB_CONNECTION", asbConn)
            .WithEnvironment("ASB_ENTITY", asbEntity)
            .WithEnvironment("ORDER_HUB_URL", ordeHubUrl)
            .Build();

        Api = new ContainerBuilder()
            .WithName($"orders-api-tests-{Guid.NewGuid()}")
            .WithImage("order-api:tests")
            .WithNetworkAliases("orders-api") 
            .WithNetwork(_network)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspEnv)
            .WithEnvironment("STRING_CONNECTION", DbConnectionForContainers)
            .WithEnvironment("ASB_CONNECTION", asbConn)
            .WithEnvironment("ASB_ENTITY", asbEntity)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithPortBinding(0, 8080)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();

        DbConnectionForTests = Postgres.GetConnectionString();
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(DbConnectionForTests)
            .Options;

        using (var context = new OrderDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await Worker.StartAsync();
        await Api.StartAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));
        if (Environment.GetEnvironmentVariable("DEBUG_TESTCONTAINERS") == "true")
        {
            Console.WriteLine("=== DEBUG_TESTCONTAINERS ===");
            Console.WriteLine($"Postgres Id: {Postgres.Id}");
            Console.WriteLine($"Worker   Id: {Worker.Id}");
            Console.WriteLine($"Api   Id: {Api.Id}");
            await Task.Delay(TimeSpan.FromSeconds(80));
        }
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        await Worker.DisposeAsync();
        await Postgres.DisposeAsync();
        await _network.DisposeAsync();
    }

    public OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(DbConnectionForTests)
            .Options;

        return new OrderDbContext(options);
    }
}
