namespace Ashlar.Provenance.Graph.Neo4j;

/// <summary>Connection settings for optional Neo4j provenance graph.</summary>
public sealed class Neo4jProvenanceGraphOptions
{
    /// <summary>Bolt URI (e.g. bolt://localhost:7687).</summary>
    public string Uri { get; set; } = "bolt://localhost:7687";

    /// <summary>Neo4j username.</summary>
    public string Username { get; set; } = "neo4j";

    /// <summary>Neo4j password.</summary>
    public string Password { get; set; } = "provenance-graph";

    /// <summary>When false, Neo4j store is not registered.</summary>
    public bool Enabled { get; set; }
}
