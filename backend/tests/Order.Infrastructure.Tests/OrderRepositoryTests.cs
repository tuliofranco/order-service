using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Core.Enums;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Testcontainers.PostgreSql;
using Xunit;
using DomainOrder = Order.Core.Domain.Entities.Order;

namespace Order.Infrastructure.Tests;

public class OrderRepositoryTests
{
    private static OrderDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    [Fact]
    public async Task AddAsync_E_SaveChangesAsync_DevePersistirOrderENaoPerderDados()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Tulio", "Boleto", 300m);


        await repo.AddAsync(order, CancellationToken.None);
        var saved = await repo.SaveChangesAsync(CancellationToken.None);


        saved.Should().BeTrue("SaveChangesAsync deve retornar true quando houve alteração");

        var loaded = await repo.GetByIdAsync(order.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(order.Id);
        loaded.ClienteNome.Should().Be("Tulio");
        loaded.Produto.Should().Be("Boleto");
        loaded.Valor.Should().Be(300m);
        loaded.Status.Should().Be(OrderStatus.Pendente);
        loaded.data_criacao.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetAllAsync_DeveRetornarEmOrdemDecrescenteDeDataCriacao()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        var older = DomainOrder.Create("Cliente A", "Produto A", 10m);
        older.data_criacao = DateTime.UtcNow.AddMinutes(-30);

        var middle = DomainOrder.Create("Cliente B", "Produto B", 20m);
        middle.data_criacao = DateTime.UtcNow.AddMinutes(-20);

        var newer = DomainOrder.Create("Cliente C", "Produto C", 30m);
        newer.data_criacao = DateTime.UtcNow.AddMinutes(-10);

        await repo.AddAsync(older);
        await repo.AddAsync(middle);
        await repo.AddAsync(newer);

        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();

        all.Should().HaveCount(3);

        all.Select(x => x.Id).Should().ContainInOrder(new[]
        {
            newer.Id,
            middle.Id,
            older.Id
        });
    }

    [Fact]
    public async Task ExistsAsync_DeveRetornarTrueSeExisteFalseSeNaoExiste()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Tulio", "Boleto", 300m);

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var exists = await repo.ExistsAsync(order.Id);
        var notExists = await repo.ExistsAsync(Guid.NewGuid());

        exists.Should().BeTrue("acabamos de salvar essa order");
        notExists.Should().BeFalse("esse Guid não está salvo");
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarNullSeNaoEncontrar()
    {
        // arrange
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        // act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkProcessingIfPendingAsync_DeveMudarDePendenteParaProcessando_E_RetornarTrue()
    {
        // Testar um método relacional, é necessário um provider relacional.
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite(conn)
            .Options;

        using var ctx = new OrderDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Cliente", "Produto", 99m);
        await repo.AddAsync(order, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var before = await repo.GetByIdAsync(order.Id, CancellationToken.None);
        before!.Status.Should().Be(OrderStatus.Pendente);

        var changed = await repo.MarkProcessingIfPendingAsync(order.Id, CancellationToken.None);

        changed.Should().BeTrue("deve mudar de Pendente para Processando uma única vez");

        var after = await repo.GetByIdAsync(order.Id, CancellationToken.None);
        after!.Status.Should().Be(OrderStatus.Processando);
    }

    [Theory]
    [InlineData(OrderStatus.Processando)]
    [InlineData(OrderStatus.Finalizado)]
    public async Task MarkProcessingIfPendingAsync_NaoDeveAlterarSeNaoEstiverPendente_E_RetornaFalse(OrderStatus initial)
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseSqlite(conn)
                .Options;

        using var ctx = new OrderDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Cliente", "Produto", 99m);
        order.Status = initial;
        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var changed = await repo.MarkProcessingIfPendingAsync(order.Id, CancellationToken.None);

        changed.Should().BeFalse($"quando status inicial é {initial}, não deve alterar");
        var after = await repo.GetByIdAsync(order.Id);
        after!.Status.Should().Be(initial);
    }

    [Fact]
    public async Task UpdateAsync_DevePersistirAlteracoes_EmEntidadeExistente()
    {
        using var ctx = new OrderDbContext(
            new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Cliente A", "Produto A", 100m);
        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        // act: altera alguns campos e persiste
        order.ClienteNome = "Cliente A+";
        order.Produto     = "Produto A+";
        order.Valor       = 150m;

        await repo.UpdateAsync(order);
        var saved = await repo.SaveChangesAsync();

        // assert
        saved.Should().BeTrue();
        var loaded = await repo.GetByIdAsync(order.Id);
        loaded!.ClienteNome.Should().Be("Cliente A+");
        loaded.Produto.Should().Be("Produto A+");
        loaded.Valor.Should().Be(150m);
    }
}