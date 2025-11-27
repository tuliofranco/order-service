using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Order.IntegrationTests.Fixtures;
using System.Text.Json;
using System.Net.Http.Json;
using Org.BouncyCastle.Asn1.Cms;
using FluentAssertions;
using Order.IntegrationTests.Golden.Dto;
using Npgsql;

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
    public async Task CreateOrder_ShouldCreateOrder_and_GetByIdWithHistoryOnStatusFinalized()
    {
        var orderToCreate = new { clienteNome = "Tulio Franco", produto = "Carro", valor = 45000m };

        var response = await _client.PostAsJsonAsync("/orders", orderToCreate);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedOrderResponse>();
        created.Should().NotBeNull();

        await Task.Delay(TimeSpan.FromSeconds(50));

        var id = created!.id;

        var getResponse = await _client.GetAsync($"/orders/{id}");
        getResponse.EnsureSuccessStatusCode();

        var json = await getResponse.Content.ReadAsStringAsync();

        var parsed = JsonSerializer.Deserialize<JsonElement>(json);

        parsed.TryGetProperty("id", out var idProp).Should().BeTrue();
        Guid.TryParse(idProp.GetString(), out _)
            .Should().BeTrue("o id deve ser um GUID válido");

        parsed.TryGetProperty("createdAtUtc", out var createdAt)
            .Should().BeTrue("deve retornar createdAtUtc");

        DateTime.TryParse(createdAt.GetString(), out _)
            .Should().BeTrue("createdAtUtc deve ser uma data válida");

        var historyArray = parsed.GetProperty("history");
        historyArray.ValueKind.Should().Be(JsonValueKind.Array);

        foreach (var h in historyArray.EnumerateArray())
        {
            Guid.TryParse(h.GetProperty("id").GetString(), out _)
                .Should().BeTrue("id do history deve ser GUID");

            Guid.TryParse(h.GetProperty("orderId").GetString(), out _)
                .Should().BeTrue("orderId deve ser GUID");

            DateTime.TryParse(h.GetProperty("occurredAt").GetString(), out _)
                .Should().BeTrue("occurredAt deve ser uma data válida");

            Guid.TryParse(h.GetProperty("correlationId").GetString(), out _)
                .Should().BeTrue("correlationId deve ser GUID");
        }

        var orderDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

        orderDict["id"] = "SANITIZED_GUID";
        orderDict["createdAtUtc"] = "SANITIZED_DATE";

        var history = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            JsonSerializer.Serialize(orderDict["history"])
        )!;
        history = history
            .OrderBy(h =>
            {
                var dateStr = h["occurredAt"]?.ToString();
                return DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.MinValue;
            })
            .ToList();

        foreach (var h in history)
        {
            h["id"] = "SANITIZED_GUID";
            h["orderId"] = "SANITIZED_GUID";
            h["occurredAt"] = "SANITIZED_DATE";
            h["correlationId"] = "SANITIZED_GUID";
        }

        orderDict["history"] = history;

        var sanitizedJson = JsonSerializer.Serialize(
            orderDict,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );
        var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName;

        var goldenPath = Path.Combine(
            projectRoot,
            "Golden",
            "Data",
            "CreateOrder_ShouldCreateOrder_and_GetByIdWithHistoryOnStatusFinalized.golden.json"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);

        var expectedJson = await File.ReadAllTextAsync(goldenPath);

        var expectedDoc = JsonDocument.Parse(expectedJson);
        var actualDoc = JsonDocument.Parse(sanitizedJson);

        JsonElement.DeepEquals(actualDoc.RootElement, expectedDoc.RootElement)
            .Should()
            .BeTrue("o JSON atual sanitizado deve ser equivalente ao golden");
    }
}

