using System.Text;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.CLI.Unity.Pipeline;

namespace Nexo.CLI.Commands;

internal sealed class IterateHandler(Func<SelfExtendRunnerAdapter> runnerFactory)
{
    public async Task<int> ExecuteAsync(
        string projectRoot,
        string changeDescription,
        string systemDir,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!UnityDevCommand.ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var fullSystemDir = Path.Combine(fullProjectRoot, systemDir);
        if (!Directory.Exists(fullSystemDir))
        {
            UnityDevCommand.WriteError($"System directory not found: {fullSystemDir}", json);
            return 1;
        }

        var existingFiles = Directory.GetFiles(fullSystemDir, "*.cs", SearchOption.AllDirectories);
        if (existingFiles.Length == 0)
        {
            UnityDevCommand.WriteError($"No .cs files found in {systemDir}", json);
            return 1;
        }

        UnityDevCommand.SetPathAllowlist(fullProjectRoot, fullSystemDir);

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Existing files in the system:");
        contextBuilder.AppendLine();

        foreach (var file in existingFiles)
        {
            var relativePath = Path.GetRelativePath(fullProjectRoot, file).Replace('\\', '/');
            var content = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            contextBuilder.AppendLine($"// FILE: {relativePath}");
            contextBuilder.AppendLine(content);
            contextBuilder.AppendLine();

            var overrideFragment = DescriptorOverrides.ToPromptFragment(file);
            if (!string.IsNullOrEmpty(overrideFragment))
                contextBuilder.AppendLine(overrideFragment);
        }

        var prompt = UnityDevCommand.BuildIteratePrompt(changeDescription, systemDir, contextBuilder.ToString());

        var runner = runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-iterate", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            UnityDevCommand.WriteError($"Iteration failed: {result.Summary}", json);
            return 1;
        }

        var writtenFiles = Directory.GetFiles(fullSystemDir, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(fullProjectRoot, f).Replace('\\', '/'))
            .ToList();

        UnityDevCommand.WriteManifest(fullSystemDir, changeDescription, writtenFiles);

        UnityDevCommand.WriteIterateResult(writtenFiles, changeDescription, systemDir, json);
        return 0;
    }
}
