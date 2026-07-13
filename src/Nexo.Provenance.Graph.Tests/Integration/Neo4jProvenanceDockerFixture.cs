using Neo4j.Driver;
using Testcontainers.Neo4j;
using Xunit;

namespace Nexo.Provenance.Graph.Tests.Integration;

/// <summary>Shared Neo4j container for provenance graph integration tests.</summary>
public sealed class Neo4jProvenanceDockerFixture : IAsyncLifetime
{
    private Neo4jContainer? _container;

    public IDriver? Driver { get; private set; }

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!ShouldRunNeo4jTests())
            return;

        _container = new Neo4jBuilder("neo4j:5.26-community")
            .WithEnvironment("NEO4J_AUTH", "neo4j/provenance-graph")
            .Build();

        await _container.StartAsync().ConfigureAwait(false);
        Driver = GraphDatabase.Driver(_container.GetConnectionString(), AuthTokens.Basic("neo4j", "provenance-graph"));
        IsAvailable = true;
    }

    public async Task DisposeAsync()
    {
        if (Driver is not null)
        {
            await Driver.DisposeAsync().ConfigureAwait(false);
            Driver = null;
        }

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }

    private static bool ShouldRunNeo4jTests()
    {
        var flag = Environment.GetEnvironmentVariable("NEXO_RUN_NEO4J_CONTAINER");
        if (string.Equals(flag, "0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }
}

[CollectionDefinition("Neo4jProvenance")]
public sealed class Neo4jProvenanceCollection : ICollectionFixture<Neo4jProvenanceDockerFixture>;
