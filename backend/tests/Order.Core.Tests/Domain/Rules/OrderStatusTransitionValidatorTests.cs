using System;
using FluentAssertions;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Rules;
using Xunit;

namespace Order.Core.Tests.Rules;

public class OrderStatusTransitionValidatorTests
{

    [Theory]
    [InlineData(null, OrderStatus.Pendente, true)]
    [InlineData(null, OrderStatus.Processando, false)]
    [InlineData(OrderStatus.Pendente, OrderStatus.Processando, true)]
    [InlineData(OrderStatus.Pendente, OrderStatus.Finalizado, false)]
    [InlineData(OrderStatus.Processando, OrderStatus.Finalizado, true)]
    [InlineData(OrderStatus.Processando, OrderStatus.Pendente, false)]
    [InlineData(OrderStatus.Finalizado, OrderStatus.Finalizado, false)]
    public void IsValid_Combinacoes_DeveRetornarEsperado(OrderStatus? fromStatus, OrderStatus toStatus, bool esperado)
    {
        var ok = OrderStatusTransitionValidator.IsValid(fromStatus, toStatus);
        ok.Should().Be(esperado);
    }

    [Fact]
    public void EnsureValid_InicialInvalido_DeveLancarComListaInicial()
    {
        Action act = () => OrderStatusTransitionValidator.EnsureValid(null, OrderStatus.Processando);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*'null' → 'Processando'. Esperado: [Pendente]*");
    }

    [Fact]
    public void EnsureValid_ComProximoDefinidoMasInvalido_DeveLancarComProximosDoMapa()
    {
        Action act = () => OrderStatusTransitionValidator.EnsureValid(OrderStatus.Pendente, OrderStatus.Finalizado);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*'Pendente' → 'Finalizado'. Esperado: [Processando]*");
    }

    [Fact]
    public void EnsureValid_ChaveNaoMapeada_DeveLancarComNenhum()
    {
        var fromStatusNaoMapeado = (OrderStatus)999;

        Action act = () => OrderStatusTransitionValidator.EnsureValid(fromStatusNaoMapeado, OrderStatus.Pendente);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*'999' → 'Pendente'. Esperado: [<nenhum>]*");
    }

    [Fact]
    public void EnsureValid_TransicaoValida_NaoDeveLancar()
    {
        Action act = () => OrderStatusTransitionValidator.EnsureValid(OrderStatus.Pendente, OrderStatus.Processando);
        act.Should().NotThrow();
    }
}
