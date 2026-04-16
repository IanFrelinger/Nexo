using System.CommandLine;
using System.Text.Json;
using Nexo.Core.Application.Paths;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runtime Studio helper commands (local agent-set tuning).
/// </summary>
public sealed class RuntimeStudioCommand : Command
{
    public RuntimeStudioCommand() : base("runtime-studio", "Runtime Studio: local agent-set helpers")
    {
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit JSON output");

        var specOpt = new Option<string>(
            "--spec",
            () => Path.Combine(".nexo", "workflow", "workflow_lab.runtime.json"),
            "Workflow lab runtime spec JSON (repo-relative or absolute).");

        var agentSetOpt = new Option<string>(
            "--agent-set",
            () => Path.Combine("apps", "runtime-studio", "config", "agent_set.local.json"),
            "Background agent-set JSON to update (repo-relative or absolute).");

        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Print planned ModelName changes without writing.");

        var applyTuneCmd = new Command(
            "apply-tune",
            "Apply the last workflow optimize winner to Runtime Studio Ollama ModelName fields (see .nexo/runtime/workflow_optimize_last.json).");
        applyTuneCmd.AddOption(jsonOpt);
        applyTuneCmd.AddOption(specOpt);
        applyTuneCmd.AddOption(agentSetOpt);
        applyTuneCmd.AddOption(dryRunOpt);
        applyTuneCmd.SetHandler(
            (bool json, string spec, string agentSet, bool dryRun) =>
            {
                Environment.Exit(ExecuteApplyTune(json, spec, agentSet, dryRun));
            },
            jsonOpt,
            specOpt,
            agentSetOpt,
            dryRunOpt);

        var statusCmd = new Command(
            "status",
            "Show last workflow optimize tune snapshot and current Ollama ModelName entries in the agent-set config.");
        statusCmd.AddOption(jsonOpt);
        statusCmd.AddOption(agentSetOpt);
        statusCmd.SetHandler(
            (bool json, string agentSet) =>
            {
                Environment.Exit(ExecuteStatus(json, agentSet));
            },
            jsonOpt,
            agentSetOpt);

        AddCommand(applyTuneCmd);
        AddCommand(statusCmd);
    }

    private static string ResolveAgentSetPath(string repoRoot, string agentSetRelativeOrAbsolute)
    {
        return Path.IsPathRooted(agentSetRelativeOrAbsolute)
            ? Path.GetFullPath(agentSetRelativeOrAbsolute)
            : Path.GetFullPath(Path.Combine(repoRoot, agentSetRelativeOrAbsolute));
    }

    private static int ExecuteStatus(bool json, string agentSet)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var agentSetPath = ResolveAgentSetPath(repoRoot, agentSet);
        var last = WorkflowOptimizeLastStore.TryRead(repoRoot);
        var ollamaAgents = RuntimeStudioAgentSetReader.TryListOllamaAgents(agentSetPath);

        if (!File.Exists(agentSetPath))
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = false,
                    repoRoot,
                    agentSetPath,
                    error = "Agent set file not found."
                }));
            }
            else
            {
                Console.Error.WriteLine($"Agent set not found: {agentSetPath}");
            }

            return 1;
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                repoRoot,
                lastTune = last,
                agentSetPath,
                ollamaAgents = ollamaAgents.Select(a => new { id = a.Id, modelName = a.ModelName }).ToArray()
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Repository: {repoRoot}");
        Console.WriteLine($"Agent set:  {agentSetPath}");
        Console.WriteLine();

        if (last is null)
        {
            Console.WriteLine("Last workflow optimize: (no .nexo/runtime/workflow_optimize_last.json yet)");
        }
        else
        {
            Console.WriteLine($"Last workflow optimize: {last.WrittenAtUtc:u}  ok={last.Ok}");
            Console.WriteLine($"  session:      {last.OptimizeRunId}");
            if (!string.IsNullOrWhiteSpace(last.WinnerCandidateId))
                Console.WriteLine($"  winner:       {last.WinnerCandidateId}");
            if (!string.IsNullOrWhiteSpace(last.ModelProfileId))
                Console.WriteLine($"  modelProfile: {last.ModelProfileId}");
            if (!string.IsNullOrWhiteSpace(last.CompositionId))
                Console.WriteLine($"  composition:  {last.CompositionId}");
            if (last.OllamaModels.Count > 0)
                Console.WriteLine($"  ollama models (winner): {string.Join(", ", last.OllamaModels)}");
        }

        Console.WriteLine();
        if (ollamaAgents.Count == 0)
        {
            Console.WriteLine("Ollama agents in agent set: (none with ModelProvider=ollama and ModelName set)");
        }
        else
        {
            Console.WriteLine("Ollama agents in agent set:");
            foreach (var row in ollamaAgents)
                Console.WriteLine($"  {row.Id}: {row.ModelName}");
        }

        return 0;
    }

    private static int ExecuteApplyTune(bool json, string spec, string agentSet, bool dryRun)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var last = WorkflowOptimizeLastStore.TryRead(repoRoot);
        if (last is null)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = false,
                    summary = "Missing .nexo/runtime/workflow_optimize_last.json; run workflow optimize first."
                }));
            }
            else
            {
                Console.Error.WriteLine(
                    "Missing .nexo/runtime/workflow_optimize_last.json; run `nexo workflow optimize` or apps/runtime-studio/scripts/optimize_agent_cluster.sh first.");
            }

            return 1;
        }

        var result = RuntimeStudioTuneApplier.Apply(repoRoot, spec, agentSet, last, dryRun);
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                updated = result.UpdatedAgentIds
            }));
        }
        else
        {
            Console.WriteLine(result.Summary);
        }

        return result.Ok ? 0 : 1;
    }
}
