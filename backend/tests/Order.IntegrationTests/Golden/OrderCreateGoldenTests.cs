using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order.Core.Domain.Entities.Enums;
using Order.IntegrationTests.Fixtures;
using Xunit;

namespace Order.IntegrationTests.Golden;

[Collection("Environment")]
public class OrderCreateGoldenTests
{
    private readonly HttpClient _client;
    private readonly EnvironmentFixture _env;

    public OrderCreateGoldenTests(EnvironmentFixture env)
    {
        _env = env;

        var port = env.Api.GetMappedPublicPort(8080);
        var baseUrl = $"http://localhost:{port}";
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    [Fact]
    public async Task CreateOrder_DeveGerarGolden_ComDadosDoBanco()
    {
        var requestBody = new
        {
            clienteNome = "Tulio Test E2E",
            produto = "Boleto",
            valor = 123.45m
        };
        var response = await _client.PostAsJsonAsync("/orders", requestBody);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var orderId = root.GetProperty("id").GetGuid();

        await Task.Delay(TimeSpan.FromSeconds(10));
        using var db = _env.CreateDbContext();

        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull("a ordem deve existir no banco após o fluxo end-to-end");

        var history = await db.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.OccurredAt)
            .ToListAsync();

        var normalized = new
        {
            order = new
            {
                id = order!.Id,
                clienteNome = order.ClienteNome,
                produto = order.Produto,
                valor = order.Valor,
                status = order.Status.ToString(),
                data_criacao = order.data_criacao,
                data_de_efetivacao = order.data_de_efetivacao
            },
            history = history.Select(h => new
            {
                id = h.Id,
                orderId = h.OrderId,
                fromStatus = h.FromStatus?.ToString(),
                toStatus = h.ToStatus.ToString(),
                occurredAtUtc = h.OccurredAt,
                correlationId = h.CorrelationId,
                source = h.Source,
                eventId = h.EventId
            }).ToArray()
        };

        var normalizedJson = JsonSerializer.Serialize(
            normalized,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var goldenDir = Path.Combine(AppContext.BaseDirectory, "Golden");
        Directory.CreateDirectory(goldenDir);

        var goldenPath = Path.Combine(goldenDir, "CreateOrderFromDatabase.json");

        await File.WriteAllTextAsync(goldenPath, normalizedJson);

        Console.WriteLine($"Golden gerado a partir do banco em: {goldenPath}");
    }



    [Fact]
    public async Task CreateOrder_Response_DeveBaterComGoldenFile()
    {
        var request = new
        {
            clienteNome = "Golden Tester",
            produto = "Boleto",
            valor = 123.45m
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var normalized = new
        {
            clienteNome = root.GetProperty("clienteNome").GetString(),
            produto = root.GetProperty("produto").GetString(),
            valor = root.GetProperty("valor").GetDecimal(),
            status = root.GetProperty("status").GetString()
        };

        var projectDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        var goldenPath = Path.Combine(projectDir, "Golden", "CreateOrderResponse.json");

        var expectedJson = await File.ReadAllTextAsync(goldenPath);

        var expected = JsonSerializer.Deserialize(
            expectedJson,
            normalized.GetType()
        )!;

        // 🔹 Serializa o normalized para JSON “bonito”
        var normalizedJson = JsonSerializer.Serialize(
            normalized,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        normalized.Should().BeEquivalentTo(expected, options =>
        {
            options
                .WithTracing()
                .WithStrictOrdering();
            return options;
        });
    }



    [Fact]
    public async Task CreateOrder_VisaoDoBanco_DeveBaterComGoldenFile()
    {
        var requestBody = new
        {
            clienteNome = "Tulio Test E2E",
            produto = "Boleto",
            valor = 123.45m
        };
        var response = await _client.PostAsJsonAsync("/orders", requestBody);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var responseDoc = JsonDocument.Parse(responseJson);
        var responseRoot = responseDoc.RootElement;
        var orderId = responseRoot.GetProperty("id").GetGuid();

        await Task.Delay(TimeSpan.FromSeconds(10));
        await using var db = _env.CreateDbContext();

        var order = await db.Orders
            .AsNoTracking()
            .SingleAsync(o => o.Id == orderId);

        var history = await db.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.OccurredAt)
            .ToListAsync();

        var actual = new
        {
            order = new
            {
                id = order.Id,
                clienteNome = order.ClienteNome,
                produto = order.Produto,
                valor = order.Valor,
                status = order.Status.ToString(),
                data_criacao = order.data_criacao,
                data_de_efetivacao = order.data_de_efetivacao
            },
            history = history.Select(h => new
            {
                id = h.Id,
                orderId = h.OrderId,
                fromStatus = h.FromStatus?.ToString(),
                toStatus = h.ToStatus.ToString(),
                occurredAtUtc = h.OccurredAt,
                correlationId = h.CorrelationId,
                source = h.Source,
                eventId = h.EventId
            }).ToList()
        };

        var projectDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        var goldenPath = Path.Combine(projectDir, "Golden", "CreateOrderFromDatabase.json");

        var expectedJson = await File.ReadAllTextAsync(goldenPath);

        var expected = JsonSerializer.Deserialize(
            expectedJson,
            actual.GetType(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        )!;

        var actualJsonPretty = JsonSerializer.Serialize(
            actual,
            new JsonSerializerOptions { WriteIndented = true });

        actual.Should().BeEquivalentTo(expected, options =>
        {
            options
                .WithTracing()
                .WithStrictOrdering()
                .Excluding(ctx =>
                    ctx.Path.EndsWith(".id") ||
                    ctx.Path.EndsWith(".orderId") ||
                    ctx.Path.EndsWith(".eventId") ||
                    ctx.Path.EndsWith(".occurredAtUtc") ||
                    ctx.Path.EndsWith(".data_criacao") ||
                    ctx.Path.EndsWith(".data_de_efetivacao") ||
                    ctx.Path.EndsWith(".correlationId")
                    );
            return options;
        });

        history.Select(h => h.ToStatus).Should().Equal(
                                        OrderStatus.Pendente,
                                        OrderStatus.Processando,
                                        OrderStatus.Finalizado);
                    history.Select(h => h.CorrelationId)
                .Distinct()
                .Should().HaveCount(1, "todos os eventos da mesma ordem devem compartilhar o mesmo correlationId");
                    
        history.Should().OnlyContain(h => h.CorrelationId == orderId.ToString(),
            "o correlationId deve ser o mesmo que o orderId para toda a cadeia de eventos");

    }
}
