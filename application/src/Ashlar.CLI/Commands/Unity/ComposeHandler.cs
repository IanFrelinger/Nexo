using System.Text;
using System.Text.Json;
using Ashlar.CLI.Unity.Pipeline;

namespace Ashlar.CLI.Commands.Unity;

/// <summary>Handles compose requests.</summary>
internal sealed class ComposeHandler(UnityGenerateExecutor executeGenerate)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string projectRoot,
        string configPath,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!UnityDevCommand.ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var fullConfigPath = Path.Combine(fullProjectRoot, configPath);
        var graph = CompositionGraph.LoadFromFile(fullConfigPath);
        if (graph == null || graph.Systems.Count == 0)
        {
            UnityDevCommand.WriteError($"Composition graph not found or empty: {fullConfigPath}", json);
            return 1;
        }

        IReadOnlyList<CompositionNode> executionOrder;
        try
        {
            executionOrder = graph.GetExecutionOrder();
        }
        catch (InvalidOperationException ex)
        {
            UnityDevCommand.WriteError(ex.Message, json);
            return 1;
        }

        var completedOutputs = new Dictionary<string, string>();
        var steps = new List<object>();

        foreach (var node in executionOrder)
        {
            if (!json) Console.WriteLine($"compose: generating system '{node.Id}'...");

            var upstreamContext = new StringBuilder();
            foreach (var dep in node.Depends)
            {
                if (completedOutputs.TryGetValue(dep, out var output))
                {
                    upstreamContext.AppendLine($"--- Output from '{dep}' ---");
                    upstreamContext.AppendLine(output);
                }
            }

            var code = await executeGenerate(
                projectRoot, node.Prompt, UnityDevCommand.DefaultOutputDir, UnityDevCommand.DefaultTestDir,
                false, json, ct,
                compositionContext: upstreamContext.ToString()).ConfigureAwait(false);

            completedOutputs[node.Id] = node.Prompt;
            steps.Add(new { systemId = node.Id, exitCode = code });

            if (code != 0)
            {
                UnityDevCommand.WriteError($"Composition step '{node.Id}' failed.", json);
                if (json)
                    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, action = "compose", steps },
                        new JsonSerializerOptions { WriteIndented = true }));
                return code;
            }
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "compose", steps },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"compose: completed {executionOrder.Count} systems.");
        }

        return 0;
    }
}
