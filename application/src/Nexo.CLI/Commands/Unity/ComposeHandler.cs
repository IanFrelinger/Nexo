using System.Text;
using System.Text.Json;
using Nexo.CLI.Unity.Pipeline;

namespace Nexo.CLI.Commands;

internal delegate Task<int> UnityGenerateExecutor(
    string projectRoot,
    string systemDescription,
    string outputDir,
    string testDir,
    bool dryRun,
    bool json,
    CancellationToken ct,
    string? templatePath = null,
    string? compositionContext = null);

internal sealed class ComposeHandler(UnityGenerateExecutor executeGenerate)
{
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
