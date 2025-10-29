using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Idempotency;
using Order.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Order.Infrastructure.Tests;

public class ProcessedMessageStoreIntegrationTests
{
    [Fact]
    public async Task TryMarkProcessedAsync_DeveInserirPrimeiraVez_ERetornarFalseNaSegunda()
    {
        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("orders_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await pgContainer.StartAsync();

        try
        {
            // 2. Monta o DbContext apontando pro Postgres do container
            //    GetConnectionString() já vem pronto no formato Host=...;Port=...;Database=...;Username=...;Password=...
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql(pgContainer.GetConnectionString())
                .Options;

            await using var ctx = new OrderDbContext(options);
            await ctx.Database.EnsureCreatedAsync();

            var store = new ProcessedMessageStore(ctx);
            var ct = CancellationToken.None;

            var messageId = "msg-que-e-um-guid";

            var primeiraVez = await store.TryMarkProcessedAsync(messageId, ct);
            var segundaVez = await store.TryMarkProcessedAsync(messageId, ct);

            // 4. Valida comportamento idempotente
            primeiraVez.Should().BeTrue("primeira inserção deve afetar 1 linha");
            segundaVez.Should().BeFalse("segunda inserção faz ON CONFLICT DO NOTHING e deve afetar 0 linhas");

            // 5. Sanity check: agora HasProcessedAsync deve responder true
            var jaProcessou = await store.HasProcessedAsync(messageId, ct);
            jaProcessou.Should().BeTrue();
        }
        finally
        {
            // 6. Para e descarta o container
            await pgContainer.DisposeAsync();
        }
    }
}
