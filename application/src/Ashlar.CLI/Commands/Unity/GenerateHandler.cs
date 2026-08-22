using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.CLI.Unity.Pipeline;

namespace Ashlar.CLI.Commands.Unity;
/// <summary>Handles generate requests.</summary>
internal sealed class GenerateHandler(Func<SelfExtendRunnerAdapter> runnerFactory)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string projectRoot,
        string systemDescription,
        string outputDir,
        string testDir,
        bool dryRun,
        bool json,
        CancellationToken ct,
        string? templatePath = null,
        string? compositionContext = null)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);

        if (!Directory.Exists(fullProjectRoot) || !Directory.Exists(Path.Combine(fullProjectRoot, "Assets")))
        {
            var projectName = Path.GetFileName(fullProjectRoot);
            if (string.IsNullOrWhiteSpace(projectName)) projectName = "AshlarForgeGame";

            if (!json) Console.WriteLine($"Project not found at {fullProjectRoot} — scaffolding new Unity project '{projectName}'...");
            var initResult = UnityDevCommand.ExecuteInit(projectRoot, projectName, json);
            if (initResult != 0) return initResult;
        }

        if (!UnityDevCommand.ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var fullOutputDir = Path.Combine(fullProjectRoot, outputDir);
        var fullTestDir = Path.Combine(fullProjectRoot, testDir);

        UnityDevCommand.SetPathAllowlist(fullProjectRoot, fullOutputDir, fullTestDir);

        var constraints = ProjectConstraints.LoadFromFile(fullProjectRoot);
        var template = templatePath != null ? GenerationTemplate.LoadFromFile(templatePath) : null;

        var prompt = UnityDevCommand.BuildGeneratePrompt(systemDescription, outputDir, testDir,
            constraints.ToPromptFragment(),
            template?.ToPromptFragment() ?? "",
            compositionContext ?? "");

        if (dryRun)
        {
            UnityDevCommand.WriteDryRunOutput(prompt, json);
            return 0;
        }

        var runner = runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-generate", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            UnityDevCommand.WriteError($"Generation failed: {result.Summary}", json);
            return 1;
        }

        Directory.CreateDirectory(fullOutputDir);
        var writtenFiles = Directory.Exists(fullOutputDir)
            ? Directory.GetFiles(fullOutputDir, "*.cs", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(fullProjectRoot, f).Replace('\\', '/'))
                .ToList()
            : new List<string>();

        if (Directory.Exists(fullTestDir))
        {
            writtenFiles.AddRange(
                Directory.GetFiles(fullTestDir, "*.cs", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(fullProjectRoot, f).Replace('\\', '/')));
        }

        UnityDevCommand.WriteManifest(fullOutputDir, systemDescription, writtenFiles);

        UnityDevCommand.WriteGenerateResult(writtenFiles, systemDescription, outputDir, json);
        return 0;
    }
}
