using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Order.Core.Domain.Repositories;
using Order.Infrastructure.Messaging.Abstractions;
using Order.Infrastructure.Persistence;
using OrderService.Infrastructure;
using Xunit;

namespace Order.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ComConnectionStringValida_RegistraServicosEsperados()
    {
        // arrange
        var services = new ServiceCollection();
        var fakeConnString =
            "Host=localhost;Database=testdb;Username=dummy;Password=dummy";

        // act
        services.AddInfrastructure(fakeConnString);

        // assert 1: DbContext foi registrado
        services.Any(d =>
            d.ServiceType == typeof(OrderDbContext)
        ).Should().BeTrue("OrderDbContext deve ser registrado");

        // assert 2: IOrderRepository foi registrado
        services.Any(d =>
            d.ServiceType == typeof(IOrderRepository)
        ).Should().BeTrue("IOrderRepository deve ser registrado");

        // assert 3: IServiceBusPublisher foi registrado
        services.Any(d =>
            d.ServiceType == typeof(IServiceBusPublisher)
        ).Should().BeTrue("IServiceBusPublisher deve ser registrado");

        // (extra) podemos montar um provider e garantir que OrderDbContext resolve,
        // porque ele tem tudo que precisa (connection string já passada)
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var dbCtx = sp.GetService<OrderDbContext>();
        dbCtx.Should().NotBeNull("OrderDbContext deve poder ser resolvido do container");
    }

    [Fact]
    public void AddInfrastructure_SemConnectionString_DeveLancarInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        Action act = () =>
        {
            services.AddInfrastructure(null);
        };

        // assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Connection string não configurada*");
    }
}
