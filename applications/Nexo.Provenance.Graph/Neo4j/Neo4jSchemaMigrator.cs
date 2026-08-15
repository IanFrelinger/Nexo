using System.Reflection;
using Neo4j.Driver;

namespace Nexo.Provenance.Graph.Neo4j;

/// <summary>Loads idempotent Cypher migration from embedded resources.</summary>
public static class Neo4jSchemaMigrator
{
    public static async Task ApplyAsync(IAsyncSession session, CancellationToken cancellationToken = default)
    {
        var cypher = await LoadMigrationScriptAsync(cancellationToken).ConfigureAwait(false);
        foreach (var statement in cypher.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(statement))
                continue;

            await session.RunAsync(statement).ConfigureAwait(false);
        }
    }

    private static async Task<string> LoadMigrationScriptAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var path = Path.Combine(AppContext.BaseDirectory, "Neo4j", "Neo4jSchemaMigration.cypher");
        if (File.Exists(path))
            return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        await using var stream = assembly.GetManifestResourceStream("Nexo.Provenance.Graph.Neo4j.Neo4jSchemaMigration.cypher");
        if (stream is null)
            throw new FileNotFoundException("Neo4j schema migration script not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}
