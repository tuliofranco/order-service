using System;
using System.Threading.Tasks;
using FluentAssertions;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Entities.Enums;
using Order.Infrastructure.Persistence;
using Order.IntegrationTests.Fixtures;
using Xunit;

namespace Order.IntegrationTests.Orders;

[Collection("Environment")]
public sealed class OrderRepositoryTests
{
    private readonly EnvironmentFixture _fixture;

    public OrderRepositoryTests(EnvironmentFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DevePersistirOrderNoBanco()
    {
        // Arrange
        await using var db = _fixture.CreateDbContext();
        var order = OrderEntity.Create("Tulio", "Boleto", 300m);

        // Act
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Assert
        var fromDb = await db.Orders.FindAsync(order.Id);
        fromDb.Should().NotBeNull();
        fromDb.Id.Should().NotBe(Guid.Empty);
        fromDb!.ClienteNome.Should().Be("Tulio");
        fromDb!.Produto.Should().Be("Boleto");
        fromDb!.Valor.Should().Be(300);
        fromDb.Status.Should().Be(OrderStatus.Pendente);
        fromDb!.data_de_efetivacao.Should().Be(null);
        fromDb!.data_criacao.Should().NotBe(null);
    }
}
