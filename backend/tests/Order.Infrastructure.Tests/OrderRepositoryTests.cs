using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Core.Domain.Entities;
using Order.Core.Enums;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
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
        // arrange
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Tulio", "Boleto", 300m);

        // act
        await repo.AddAsync(order, CancellationToken.None);
        var saved = await repo.SaveChangesAsync(CancellationToken.None);

        // assert
        saved.Should().BeTrue("SaveChangesAsync deve retornar true quando houve alteração");

        // vamos garantir que está realmente no 'banco'
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
        // arrange
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        // Três orders com datas diferentes pra testar a ordenação
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

        // act
        var all = await repo.GetAllAsync();

        // assert
        all.Should().HaveCount(3);

        // o mais novo primeiro
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
        // arrange
        using var ctx = CreateInMemoryContext();
        var repo = new OrderRepository(ctx);

        var order = DomainOrder.Create("Tulio", "Boleto", 300m);

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        // act
        var exists = await repo.ExistsAsync(order.Id);
        var notExists = await repo.ExistsAsync(Guid.NewGuid());

        // assert
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
}
