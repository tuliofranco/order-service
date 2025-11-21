using Xunit;

namespace Order.IntegrationTests.Fixtures;

[CollectionDefinition("Environment")]
public sealed class OrderServicePostgresCollection 
    : ICollectionFixture<EnvironmentFixture>
{
}
