using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Workflow;
/// <summary>Handles scaffold requests.</summary>
internal sealed class ScaffoldHandler
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(string outputPath, bool force, bool json)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            WriteResult(new WorkflowScaffoldResult(false, "Output path is required."), json);
            return Task.FromResult(1);
        }

        var fullPath = Path.GetFullPath(outputPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (File.Exists(fullPath) && !force)
        {
            WriteResult(new WorkflowScaffoldResult(false, $"File already exists: {fullPath}. Use --force to overwrite.", fullPath), json);
            return Task.FromResult(1);
        }

        var scaffold = WorkflowLabRuntimeSpec.Default();
        var payload = JsonSerializer.Serialize(scaffold, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullPath, payload);
        WriteResult(new WorkflowScaffoldResult(true, "Workflow lab spec scaffolded successfully.", fullPath), json);
        return Task.FromResult(0);
    }

    private static void WriteResult(WorkflowScaffoldResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                outputPath = result.OutputPath
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow scaffold: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
            Console.WriteLine($"  output={result.OutputPath}");
    }

    private sealed record WorkflowScaffoldResult(bool Ok, string Summary, string? OutputPath = null);
}
