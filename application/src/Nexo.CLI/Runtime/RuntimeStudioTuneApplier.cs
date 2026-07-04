using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nexo.CLI.Runtime;

/// <summary>
/// Maps the last workflow-lab winner onto Runtime Studio <c>agent_set</c> Ollama <c>ModelName</c> fields.
/// </summary>
public static class RuntimeStudioTuneApplier
{
    private static string ResolveUnderRepo(string repoRoot, string relativeOrAbsolute) =>
        Path.IsPathRooted(relativeOrAbsolute)
            ? Path.GetFullPath(relativeOrAbsolute)
            : Path.GetFullPath(Path.Combine(repoRoot, relativeOrAbsolute));

    private static readonly (string BackgroundAgentId, string LabAgentId)[] PlannerWorkerMap =
    {
        ("runtime-planner", "planner-1"),
        ("runtime-worker-optimizer", "builder-1")
    };

    /// <summary>Creates a new Apply instance.</summary>
    public static RuntimeStudioTuneApplyResult Apply(
        string repoRoot,
        string specPath,
        string agentSetPath,
        WorkflowOptimizeLastPayload last,
        bool dryRun)
    {
        if (!last.Ok)
        {
            return new RuntimeStudioTuneApplyResult(
                false,
                "Last workflow optimize did not report a successful winner (workflow_optimize_last.json Ok=false). " +
                "Fix Ollama/models or increase --budget-runs, then re-run optimize.");
        }

        if (string.IsNullOrWhiteSpace(last.ModelProfileId) || string.IsNullOrWhiteSpace(last.CompositionId))
        {
            return new RuntimeStudioTuneApplyResult(
                false,
                "Last optimize payload is missing modelProfileId or compositionId.");
        }

        var fullSpec = ResolveUnderRepo(repoRoot, specPath);
        if (!File.Exists(fullSpec))
            return new RuntimeStudioTuneApplyResult(false, $"Workflow lab spec not found: {fullSpec}");

        WorkflowLabRuntimeSpec spec;
        try
        {
            spec = WorkflowLabRuntimeSpecLoader.Load(fullSpec, null);
        }
        catch (Exception ex)
        {
            return new RuntimeStudioTuneApplyResult(false, $"Failed to load workflow lab spec: {ex.Message}");
        }

        var composition = spec.Compositions.FirstOrDefault(c =>
            string.Equals(c.Id, last.CompositionId, StringComparison.OrdinalIgnoreCase));
        if (composition is null)
        {
            return new RuntimeStudioTuneApplyResult(
                false,
                $"Composition '{last.CompositionId}' not found in spec. Known: {string.Join(", ", spec.Compositions.Select(c => c.Id))}");
        }

        var profile = spec.ModelProfiles.FirstOrDefault(p =>
            string.Equals(p.Id, last.ModelProfileId, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            return new RuntimeStudioTuneApplyResult(
                false,
                $"Model profile '{last.ModelProfileId}' not found in spec. Known: {string.Join(", ", spec.ModelProfiles.Select(p => p.Id))}");
        }

        var fullAgentSet = ResolveUnderRepo(repoRoot, agentSetPath);
        if (!File.Exists(fullAgentSet))
            return new RuntimeStudioTuneApplyResult(false, $"Agent set file not found: {fullAgentSet}");

        var agentJson = File.ReadAllText(fullAgentSet);
        var root = JsonNode.Parse(agentJson) as JsonObject;
        if (root is null)
            return new RuntimeStudioTuneApplyResult(false, "Agent set JSON could not be parsed as an object.");

        var agents = root["BackgroundAgents"]?["Agents"] as JsonArray;
        if (agents is null)
            return new RuntimeStudioTuneApplyResult(false, "Agent set is missing BackgroundAgents.Agents array.");

        var fallbackModel = last.OllamaModels.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))?.Trim();
        var updated = new List<string>();

        foreach (var node in agents.OfType<JsonObject>())
        {
            var id = node["Id"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var provider = node["ModelProvider"]?.GetValue<string>() ?? string.Empty;
            if (!string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
                continue;

            var model = ResolveOllamaModelForBackgroundAgent(id, composition, profile, fallbackModel);
            if (string.IsNullOrWhiteSpace(model))
                continue;

            var current = node["ModelName"]?.GetValue<string>();
            if (string.Equals(current, model, StringComparison.Ordinal))
                continue;

            node["ModelName"] = model;
            updated.Add($"{id}={model}");
        }

        if (updated.Count == 0)
        {
            return new RuntimeStudioTuneApplyResult(
                true,
                "No Ollama agents required updates (already aligned or no mappable agents).");
        }

        if (dryRun)
        {
            return new RuntimeStudioTuneApplyResult(
                true,
                $"Dry run: would update {updated.Count} agent(s): {string.Join("; ", updated)}",
                updated);
        }

        var outText = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullAgentSet, outText + Environment.NewLine);
        return new RuntimeStudioTuneApplyResult(
            true,
            $"Updated {updated.Count} Ollama agent(s): {string.Join("; ", updated)}",
            updated);
    }

    internal static string? ResolveOllamaModelForBackgroundAgent(
        string backgroundAgentId,
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile,
        string? fallbackModel)
    {
        var labId = PlannerWorkerMap.FirstOrDefault(x =>
            string.Equals(x.BackgroundAgentId, backgroundAgentId, StringComparison.OrdinalIgnoreCase)).LabAgentId;

        if (!string.IsNullOrWhiteSpace(labId))
        {
            var resolved = ResolveOllamaModelForLabAgent(composition, profile, labId);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        if (string.Equals(profile.Default.Provider, "ollama", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(profile.Default.Model))
            return profile.Default.Model.Trim();

        return string.IsNullOrWhiteSpace(fallbackModel) ? null : fallbackModel.Trim();
    }

    internal static string? ResolveOllamaModelForLabAgent(
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile,
        string labAgentId)
    {
        if (profile.Agents.TryGetValue(labAgentId, out var agentSpec) &&
            string.Equals(agentSpec.Provider, "ollama", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(agentSpec.Model))
            return agentSpec.Model.Trim();

        var role = composition.Roles.FirstOrDefault(r =>
            string.Equals(r.AgentId, labAgentId, StringComparison.OrdinalIgnoreCase));
        if (role is not null)
        {
            if (!string.IsNullOrWhiteSpace(role.OllamaModel))
                return role.OllamaModel.Trim();
            if (profile.AgentModelHints.TryGetValue(labAgentId, out var hint) && !string.IsNullOrWhiteSpace(hint))
                return hint.Trim();
        }

        if (string.Equals(profile.Default.Provider, "ollama", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(profile.Default.Model))
            return profile.Default.Model.Trim();

        return null;
    }
}
