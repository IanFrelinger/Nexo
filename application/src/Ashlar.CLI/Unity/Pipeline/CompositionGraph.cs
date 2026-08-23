namespace Ashlar.CLI.Unity.Pipeline;

using System.Text.Json;

/// <summary>
/// Defines a dependency graph of systems to generate. Each system
/// has a prompt and depends on other systems. Generation runs in
/// topological order — downstream systems receive upstream output
/// as context.
/// </summary>
public sealed record CompositionGraph
{
    /// <summary>Composition nodes defining prompts and dependency edges.</summary>
    public IReadOnlyList<CompositionNode> Systems { get; init; } = Array.Empty<CompositionNode>();

    /// <summary>Loads a composition graph from a JSON file.</summary>
    public static CompositionGraph? LoadFromFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CompositionGraph>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Returns nodes in topological order. Throws if a cycle is detected.
    /// </summary>
    public IReadOnlyList<CompositionNode> GetExecutionOrder()
    {
        var remaining = new HashSet<string>(Systems.Select(s => s.Id));
        var completed = new HashSet<string>();
        var result = new List<CompositionNode>();
        var lookup = Systems.ToDictionary(s => s.Id);

        while (remaining.Count > 0)
        {
            var ready = remaining
                .Where(id => lookup[id].Depends.All(d => completed.Contains(d)))
                .ToList();

            if (ready.Count == 0)
                throw new InvalidOperationException(
                    $"Circular dependency detected among: {string.Join(", ", remaining)}");

            foreach (var id in ready)
            {
                result.Add(lookup[id]);
                remaining.Remove(id);
                completed.Add(id);
            }
        }

        return result;
    }
}
