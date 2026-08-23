using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4j.Driver;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.Neo4j;
using Ashlar.Provenance.Graph.Null;
using Ashlar.Provenance.Graph.Ports;

namespace Ashlar.Provenance.Graph.Sdk.Extensions;

/// <summary>DI registration for optional provenance graph projection.</summary>
public static class ProvenanceGraphServiceCollectionExtensions
{
    /// <summary>Register null provenance graph (default — no Neo4j dependency).</summary>
    public static IServiceCollection AddProvenanceGraph(this IServiceCollection services)
    {
        services.TryAddSingleton<IProvenanceChainHeadAuthority, UnavailableProvenanceChainHeadAuthority>();
        services.TryAddSingleton<IProvenanceGraphStore, NullProvenanceGraphStore>();
        services.TryAddSingleton<IProvenanceGraphQueries, NullProvenanceGraphQueries>();
        services.TryAddSingleton<ProvenanceProjector>();
        return services;
    }

    /// <summary>Register Neo4j-backed provenance graph when enabled in options.</summary>
    public static IServiceCollection AddNeo4jProvenanceGraph(this IServiceCollection services, Neo4jProvenanceGraphOptions options)
    {
        if (!options.Enabled)
            return services.AddProvenanceGraph();

        services.AddSingleton(options);
        services.AddSingleton<IDriver>(_ =>
            GraphDatabase.Driver(options.Uri, AuthTokens.Basic(options.Username, options.Password)));
        services.TryAddSingleton<IProvenanceChainHeadAuthority, UnavailableProvenanceChainHeadAuthority>();
        services.AddSingleton<IProvenanceGraphStore, Neo4jProvenanceGraphStore>();
        services.AddSingleton<IProvenanceGraphQueries, Neo4jProvenanceGraphQueries>();
        services.TryAddSingleton<ProvenanceProjector>();
        return services;
    }
}
