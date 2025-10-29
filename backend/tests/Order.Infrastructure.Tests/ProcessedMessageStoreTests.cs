using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Idempotency;
using Order.Infrastructure.Persistence;
using Xunit;

namespace Order.Infrastructure.Tests;

public class ProcessedMessageStoreTests
{
    private static OrderDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    [Fact]
    public async Task HasProcessedAsync_DeveRetornarTrueQuandoMensagemJaFoiProcessada()
    {
        // arrange
        using var ctx = CreateInMemoryContext();

        ctx.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = "msg-123",
            ProcessedAtUtc = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        var store = new ProcessedMessageStore(ctx);

        // act
        var result = await store.HasProcessedAsync("msg-123", CancellationToken.None);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasProcessedAsync_DeveRetornarFalseQuandoMensagemNaoFoiProcessada()
    {
        // arrange
        using var ctx = CreateInMemoryContext();
        var store = new ProcessedMessageStore(ctx);

        // act
        var result = await store.HasProcessedAsync("msg-nao-existe", CancellationToken.None);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasProcessedAsync_DeveRetornarFalseParaMessageIdDiferente()
    {
        // arrange
        using var ctx = CreateInMemoryContext();

        ctx.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = "msg-a",
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        await ctx.SaveChangesAsync();

        var store = new ProcessedMessageStore(ctx);

        // act
        var result = await store.HasProcessedAsync("msg-b", CancellationToken.None);

        // assert
        result.Should().BeFalse("msg-b não existe na tabela ainda");
    }
}
