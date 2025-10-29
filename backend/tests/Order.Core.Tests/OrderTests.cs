using System;
using FluentAssertions;
using Order.Core.Enums;

using DomainOrder = Order.Core.Domain.Entities.Order;

namespace Order.Core.Tests;

public class OrderTests
{
    [Fact]
    public void Create_DeveRetornarOrderValida_ComStatusPendente()
    {
        // arrange
        var nome = "Tulio";
        var produto = "Boleto";
        var valor = 300m;

        // act
        var order = DomainOrder.Create(nome, produto, valor);

        // assert
        order.Should().NotBeNull();

        order.Id.Should().NotBe(Guid.Empty, "toda nova ordem tem que gerar um Id");

        order.ClienteNome.Should().Be(nome);
        order.Produto.Should().Be(produto);
        order.Valor.Should().Be(valor);

        order.Status.Should().Be(OrderStatus.Pendente,
            "toda ordem recém criada começa como Pendente");

        order.data_criacao.Should()
            .BeOnOrBefore(DateTime.UtcNow)
            .And
            .BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Create_DeveGerarIdsDiferentesEDataDiferente_ParaChamadasDiferentes()
    {
        // arrange / act
        var o1 = DomainOrder.Create("Tulio", "Boleto", 300m);
        var o2 = DomainOrder.Create("Tulio", "Boleto", 300m);

        // assert
        o1.Id.Should().NotBe(o2.Id, "cada ordem precisa ter um Id único");

        o1.data_criacao.Should().NotBe(o2.data_criacao,
            "ordens criadas separadamente não devem compartilhar o mesmo timestamp exato");
    }
}
