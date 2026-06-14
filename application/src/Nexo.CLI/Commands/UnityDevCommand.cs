using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.CLI.Unity.Pipeline;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command that automates Unity gameplay system development via LLM-driven code generation.
/// Supports generating new gameplay systems, iterating on existing ones, and listing generated systems.
/// </summary>
public sealed partial class UnityDevCommand : Command
{
    private readonly Func<SelfExtendRunnerAdapter> _runnerFactory;
    private readonly GenerateHandler _generateHandler;
    private readonly IterateHandler _iterateHandler;
    private readonly AssetsHandler _assetsHandler;
    private readonly QaHandler _qaHandler;
    private readonly ComposeHandler _composeHandler;
    private readonly FullstackHandler _fullstackHandler;

    internal const string DefaultOutputDir = "Assets/Scripts/Generated";
    internal const string DefaultTestDir = "Assets/Tests/EditMode/Generated";
    internal const string ManifestFileName = ".nexo-gen-manifest.json";
    internal const string FileMarkerPrefix = "// FILE: ";

    internal const string UnitySystemPrompt = @"You are a Unity C# code generator specializing in production-quality gameplay systems.

Rules:
- Generate production-quality Unity C# code ready for use in a Unity project.
- Use MonoBehaviour for components attached to GameObjects.
- Use ScriptableObject for data containers and configuration assets.
- Use interfaces for abstraction and to define contracts between systems.
- Follow Unity naming conventions: PascalCase for types and public members, camelCase with underscore prefix (_fieldName) for private fields.
- Use [SerializeField] for inspector-exposed private fields. Never make fields public solely for inspector access.
- Include XML doc comments on all public types and members.
- Reference UnityEngine and UnityEngine.InputSystem namespaces as appropriate.
- Separate each file with a marker line in the format: // FILE: relative/path/FileName.cs
- The first line of output must be a // FILE: marker.
- Generate corresponding edit-mode unit tests when the user requests tests. Place tests under the test directory with [Test] attributes and NUnit assertions.
- Do not generate #region blocks. Keep files focused and small.
- Use namespaces matching the folder structure.
- Prefer composition over inheritance for gameplay logic.";

    public UnityDevCommand(Func<SelfExtendRunnerAdapter> runnerFactory)
        : base("unity-dev", "Automate Unity gameplay system development with LLM-driven code generation.")
    {
        _runnerFactory = runnerFactory ?? throw new ArgumentNullException(nameof(runnerFactory));
        _generateHandler = new GenerateHandler(_runnerFactory);
        _iterateHandler = new IterateHandler(_runnerFactory);
        _assetsHandler = new AssetsHandler(_runnerFactory);
        _qaHandler = new QaHandler(_runnerFactory);
        _composeHandler = new ComposeHandler(ExecuteGenerateAsync);
        _fullstackHandler = new FullstackHandler(ExecuteGenerateAsync, ExecuteAssetsAsync, ExecuteQaAsync);

        AddCommand(CreateInitCommand());
        AddCommand(CreateGenerateCommand());
        AddCommand(CreateIterateCommand());
        AddCommand(CreateListCommand());
        AddCommand(CreateAssetsCommand());
        AddCommand(CreateQaCommand());
        AddCommand(CreateFullstackCommand());
        AddCommand(CreatePinCommand());
        AddCommand(CreateComposeCommand());
    }

    internal async Task<int> ExecuteGenerateAsync(
        string projectRoot,
        string systemDescription,
        string outputDir,
        string testDir,
        bool dryRun,
        bool json,
        CancellationToken ct,
        string? templatePath = null,
        string? compositionContext = null) =>
        await _generateHandler.ExecuteAsync(projectRoot, systemDescription, outputDir, testDir, dryRun, json, ct, templatePath, compositionContext).ConfigureAwait(false);

    internal async Task<int> ExecuteIterateAsync(
        string projectRoot,
        string changeDescription,
        string systemDir,
        bool json,
        CancellationToken ct) =>
        await _iterateHandler.ExecuteAsync(projectRoot, changeDescription, systemDir, json, ct).ConfigureAwait(false);

    internal static int ExecuteList(string projectRoot, bool json) => UnityDevCommandUtilities.ExecuteList(projectRoot, json);

    internal static bool ValidateProjectRoot(string fullProjectRoot, bool json) => UnityDevCommandUtilities.ValidateProjectRoot(fullProjectRoot, json);

    internal static int ExecuteInit(string projectRoot, string projectName, bool json) => UnityDevCommandUtilities.ExecuteInit(projectRoot, projectName, json);

    internal static List<(string RelativePath, string Content)> ParseFiles(string rawOutput) => UnityDevCommandUtilities.ParseFiles(rawOutput);

    internal static void WriteManifest(string directory, string prompt, List<string> files) => UnityDevCommandUtilities.WriteManifest(directory, prompt, files);

    internal static string BuildGeneratePrompt(string systemDescription, string outputDir, string testDir, string constraintFragment = "", string templateFragment = "", string compositionContext = "") => UnityDevCommandUtilities.BuildGeneratePrompt(systemDescription, outputDir, testDir, constraintFragment, templateFragment, compositionContext);

    internal static string BuildIteratePrompt(string changeDescription, string systemDir, string existingContext) => UnityDevCommandUtilities.BuildIteratePrompt(changeDescription, systemDir, existingContext);

    internal static void SetPathAllowlist(string projectRoot, params string[] additionalPaths) => UnityDevCommandUtilities.SetPathAllowlist(projectRoot, additionalPaths);

    internal static void WriteError(string message, bool json) => UnityDevCommandUtilities.WriteError(message, json);

    internal static void WriteDryRunOutput(string prompt, bool json) => UnityDevCommandUtilities.WriteDryRunOutput(prompt, json);

    internal static void WriteGenerateResult(List<string> files, string systemDescription, string outputDir, bool json) => UnityDevCommandUtilities.WriteGenerateResult(files, systemDescription, outputDir, json);

    internal static void WriteIterateResult(List<string> files, string changeDescription, string systemDir, bool json) => UnityDevCommandUtilities.WriteIterateResult(files, changeDescription, systemDir, json);

    // ───────────────────────────────────────────────────────────────────
    //  pin subcommand
    // ───────────────────────────────────────────────────────────────────

    internal static int ExecutePin(string projectRoot, string file, string field, string? value, bool unpin, bool json) => UnityDevCommandUtilities.ExecutePin(projectRoot, file, field, value, unpin, json);

    // ───────────────────────────────────────────────────────────────────
    //  compose subcommand
    // ───────────────────────────────────────────────────────────────────

    internal async Task<int> ExecuteComposeAsync(
        string projectRoot,
        string configPath,
        bool json,
        CancellationToken ct) =>
        await _composeHandler.ExecuteAsync(projectRoot, configPath, json, ct).ConfigureAwait(false);

    // ───────────────────────────────────────────────────────────────────
    //  assets subcommand
    // ───────────────────────────────────────────────────────────────────

    internal async Task<int> ExecuteAssetsAsync(
        string projectRoot,
        string assetType,
        string description,
        bool json,
        CancellationToken ct) =>
        await _assetsHandler.ExecuteAsync(projectRoot, assetType, description, json, ct).ConfigureAwait(false);

    internal static string BuildAssetPrompt(string assetType, string description) => UnityDevCommandUtilities.BuildAssetPrompt(assetType, description);

    internal static void WriteAssetsResult(List<string> files, string assetType, string description, bool json) => UnityDevCommandUtilities.WriteAssetsResult(files, assetType, description, json);

    // ───────────────────────────────────────────────────────────────────
    //  qa subcommand
    // ───────────────────────────────────────────────────────────────────

    internal async Task<int> ExecuteQaAsync(
        string projectRoot,
        int maxIterations,
        bool json,
        string? unityPath,
        CancellationToken ct) =>
        await _qaHandler.ExecuteAsync(projectRoot, maxIterations, json, unityPath, ct).ConfigureAwait(false);

    internal static Task<(int exitCode, string output)> RunUnityCommand(string unityPath, string projectRoot, string args, CancellationToken ct) => UnityDevCommandUtilities.RunUnityCommand(unityPath, projectRoot, args, ct);

    internal static string? FindUnityEditor() => UnityDevCommandUtilities.FindUnityEditor();

    internal static void WriteQaResult(List<object> iterations, int totalIterations, bool allPassed, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = allPassed,
                action = "qa",
                totalIterations,
                allPassed,
                iterations
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev qa: {(allPassed ? "ok" : "failed")}");
            Console.WriteLine($"Iterations used: {totalIterations}");
            Console.WriteLine($"All tests passed: {allPassed}");
        }
    }

    internal static string Truncate(string text, int maxLength) => UnityDevCommandUtilities.Truncate(text, maxLength);

    // ───────────────────────────────────────────────────────────────────
    //  fullstack subcommand
    // ───────────────────────────────────────────────────────────────────

    internal async Task<int> ExecuteFullstackAsync(
        string projectRoot,
        string gameDescription,
        bool json,
        CancellationToken ct) =>
        await _fullstackHandler.ExecuteAsync(projectRoot, gameDescription, json, ct).ConfigureAwait(false);

}
