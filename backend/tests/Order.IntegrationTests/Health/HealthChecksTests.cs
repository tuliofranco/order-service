using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Order.IntegrationTests.Fixtures;
using Xunit;

namespace Order.IntegrationTests.Health;

[Collection("Environment")]
public class HealthChecksTests
{
    private readonly HttpClient _client;

    public HealthChecksTests(EnvironmentFixture env)
    {
        var port = env.Api.GetMappedPublicPort(8080);
        var baseUrl = $"http://localhost:{port}";

        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    [Fact]
    public async Task Health_DeveRetornarHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.GetProperty("status").GetString();

        status.Should().Be("Healthy");
    }
}
