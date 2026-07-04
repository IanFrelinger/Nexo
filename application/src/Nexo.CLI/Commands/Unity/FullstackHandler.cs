using System.Text.Json;

namespace Nexo.CLI.Commands.Unity;
/// <summary>Handles fullstack requests.</summary>
internal sealed class FullstackHandler(
    UnityGenerateExecutor executeGenerate,
    Func<string, string, string, bool, CancellationToken, Task<int>> executeAssets,
    Func<string, int, bool, string?, CancellationToken, Task<int>> executeQa)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string projectRoot,
        string gameDescription,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        var steps = new List<object>();

        if (!json) Console.WriteLine("fullstack: step 1/4 — init");
        var initCode = UnityDevCommand.ExecuteInit(projectRoot, Path.GetFileName(fullProjectRoot) ?? "NexoForgeGame", json);
        steps.Add(new { step = "init", exitCode = initCode });
        if (initCode != 0) { WriteResult(steps, false, json); return initCode; }

        if (!json) Console.WriteLine("fullstack: step 2/4 — generate");
        var genCode = await executeGenerate(
            projectRoot, gameDescription, UnityDevCommand.DefaultOutputDir, UnityDevCommand.DefaultTestDir,
            false, json, ct).ConfigureAwait(false);
        steps.Add(new { step = "generate", exitCode = genCode });
        if (genCode != 0) { WriteResult(steps, false, json); return genCode; }

        if (!json) Console.WriteLine("fullstack: step 3/4 — assets");
        var matCode = await executeAssets(
            projectRoot, "material", $"Materials for: {gameDescription}", json, ct).ConfigureAwait(false);
        steps.Add(new { step = "assets-material", exitCode = matCode });

        var prefabCode = await executeAssets(
            projectRoot, "prefab", $"Prefabs for: {gameDescription}", json, ct).ConfigureAwait(false);
        steps.Add(new { step = "assets-prefab", exitCode = prefabCode });

        if (!json) Console.WriteLine("fullstack: step 4/4 — qa");
        var qaCode = await executeQa(projectRoot, 5, json, null, ct).ConfigureAwait(false);
        steps.Add(new { step = "qa", exitCode = qaCode });

        var allOk = initCode == 0 && genCode == 0 && qaCode == 0;
        /// <summary>Write result.</summary>
        WriteResult(steps, allOk, json);
        return allOk ? 0 : 1;
    }

    private static void WriteResult(List<object> steps, bool success, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = success,
                action = "fullstack",
                steps
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev fullstack: {(success ? "ok" : "failed")}");
            foreach (var step in steps)
                Console.WriteLine($"  {JsonSerializer.Serialize(step)}");
        }
    }
}
