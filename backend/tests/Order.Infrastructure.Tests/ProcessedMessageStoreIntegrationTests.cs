// File: Order.Infrastructure.Tests/ProcessedMessageStoreIntegrationTests.cs

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Idempotency;
using Order.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Order.Infrastructure.Tests;

public class ProcessedMessageStoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orders_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private DbContextOptions<OrderDbContext> _options = default!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        _options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;

        // aplica schema (migrations)
        await using var ctx = new OrderDbContext(_options);
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _pg.DisposeAsync().AsTask();

    [Fact]
    public async Task TryMarkProcessedAsync_DeveInserirPrimeiraVez_ERetornarFalseNaSegunda()
    {
        // arrange
        var ct = CancellationToken.None;
        var messageId = "msg-que-e-um-guid";

        await using var ctx = new OrderDbContext(_options);
        var store = new ProcessedMessageStore(ctx);

        // act
        var primeiraVez = await store.TryMarkProcessedAsync(messageId, ct);
        var segundaVez  = await store.TryMarkProcessedAsync(messageId, ct);

        // assert
        primeiraVez.Should().BeTrue("primeira inserção deve afetar 1 linha");
        segundaVez.Should().BeFalse("segunda inserção faz ON CONFLICT DO NOTHING");

        // sanity check
        (await store.HasProcessedAsync(messageId, ct)).Should().BeTrue();
    }

    [Fact]
    public async Task TryMarkProcessedAsync_DeveSerAtomico_EmChamadasConcorrentes()
    {
        // arrange
        var ct = CancellationToken.None;
        var messageId = Guid.NewGuid().ToString();

        const int concurrency = 10;

        // act: várias tasks concorrentes tentando reservar o mesmo messageId
        var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
        {
            await using var db = new OrderDbContext(_options);
            var store = new ProcessedMessageStore(db);
            return await store.TryMarkProcessedAsync(messageId, ct);
        }).ToArray();

        await Task.WhenAll(tasks);

        // assert: somente UMA retorna true; demais false
        var trues = tasks.Count(t => t.Result);
        trues.Should().Be(1, "apenas uma reserva deve vencer a corrida");

        // e somente um registro existe na tabela
        await using var verifyDb = new OrderDbContext(_options);
        var count = await verifyDb.ProcessedMessages.CountAsync(x => x.MessageId == messageId, ct);
        count.Should().Be(1);
    }
}
