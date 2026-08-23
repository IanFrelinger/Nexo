using System.Text.Json.Nodes;

namespace Ashlar.CLI.Runtime;

/// <summary>
/// Reads Runtime Studio background agent-set JSON for reporting (Ollama rows).
/// </summary>
public static class RuntimeStudioAgentSetReader
{
    /// <summary>Creates a new OllamaAgentRow instance.</summary>
    public sealed record OllamaAgentRow(string Id, string ModelName);

    /// <summary>
    /// Returns Ollama agents with a non-empty ModelName, sorted by Id.
    /// </summary>
    public static IReadOnlyList<OllamaAgentRow> TryListOllamaAgents(string agentSetFullPath)
    {
        if (!File.Exists(agentSetFullPath))
            return Array.Empty<OllamaAgentRow>();

        List<OllamaAgentRow> rows;
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(agentSetFullPath)) as JsonObject;
            var agents = root?["BackgroundAgents"]?["Agents"] as JsonArray;
            if (agents is null)
                return Array.Empty<OllamaAgentRow>();

            rows = new List<OllamaAgentRow>();
            foreach (var node in agents.OfType<JsonObject>())
            {
                var id = node["Id"]?.GetValue<string>();
                var provider = node["ModelProvider"]?.GetValue<string>() ?? string.Empty;
                var model = node["ModelName"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(model))
                    continue;
                rows.Add(new OllamaAgentRow(id.Trim(), model.Trim()));
            }
        }
        catch
        {
            return Array.Empty<OllamaAgentRow>();
        }

        return rows.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
