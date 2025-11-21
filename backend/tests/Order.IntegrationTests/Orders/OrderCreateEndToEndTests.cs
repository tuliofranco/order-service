using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationTests.Fixtures;
using Order.Core.Domain.Entities.Enums;
using Xunit;

namespace Order.IntegrationTests.Orders;

[Collection("Environment")]
public class OrderCreateEndToEndTests
{
    private readonly HttpClient _client;
    private readonly EnvironmentFixture _env;
    private sealed record CreateOrderResponse(
        Guid Id,
        string ClienteNome,
        string Produto,
        decimal Valor,
        string Status
    );

    public OrderCreateEndToEndTests(EnvironmentFixture env)
    {
        _env = env;
        var port = env.Api.GetMappedPublicPort(8080);
        var baseUrl = $"http://localhost:{port}";

        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    [Fact]
    public async Task CreateOrder_DeveSerFinalizado_AposProcessamentoDoWorker()
    {
        var requestBody = new
        {
            clienteNome = "Tulio Test E2E",
            produto = "Boleto",
            valor = 123.45m
        };

        var response = await _client.PostAsJsonAsync("/orders", requestBody);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        created.Should().NotBeNull();
        var orderId = created!.Id;

        await Task.Delay(TimeSpan.FromSeconds(8));

        await using var db = _env.CreateDbContext();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull("a ordem criada deve existir no banco");
        order!.Status.Should().Be(OrderStatus.Finalizado,
            "após o processamento assíncrono o status deve ser Finalizado");
    }
}
