using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationTests.Fixtures;
using Order.Core.Domain.Entities.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Order.IntegrationTests.Orders;

[Collection("Environment")]
public class OrderCreateEndToEndTests
{
    private readonly HttpClient _client;
    private readonly EnvironmentFixture _env;
    private readonly ITestOutputHelper _output;
    private sealed record CreateOrderResponse(
        Guid Id,
        string ClienteNome,
        string Produto,
        decimal Valor,
        string Status
    );

    public OrderCreateEndToEndTests(EnvironmentFixture env, ITestOutputHelper output)
    {
        _env = env;
        _output = output;
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
        _output.WriteLine($"[TEST] Ordem lida do banco:");
        _output.WriteLine($"       Id={created?.Id}");

        await Task.Delay(TimeSpan.FromSeconds(20));

        await using var db = _env.CreateDbContext();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        _output.WriteLine($"[TEST] Ordem lida do banco:");
        _output.WriteLine($"       Id={order?.Id}");
        _output.WriteLine($"       Status={order?.Status}");
        _output.WriteLine($"       data_criacao={order?.data_criacao:O}");
        _output.WriteLine($"       data_de_efetivacao={order?.data_de_efetivacao:O}");
        await Task.Delay(TimeSpan.FromSeconds(50));
        order.Should().NotBeNull("a ordem criada deve existir no banco");
        order!.Status.Should().Be(OrderStatus.Finalizado,
            "após o processamento assíncrono o status deve ser Finalizado");
    }
}
