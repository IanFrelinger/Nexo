using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Text.Json;
using Nexo.BackgroundAgents.HostRunners;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command that automates Unity gameplay system development via LLM-driven code generation.
/// Supports generating new gameplay systems, iterating on existing ones, and listing generated systems.
/// </summary>
public sealed class UnityDevCommand : Command
{
    private readonly Func<SelfExtendRunnerAdapter> _runnerFactory;

    private const string DefaultOutputDir = "Assets/Scripts/Generated";
    private const string DefaultTestDir = "Assets/Tests/EditMode/Generated";
    private const string ManifestFileName = ".nexo-gen-manifest.json";
    private const string FileMarkerPrefix = "// FILE: ";

    private const string UnitySystemPrompt = @"You are a Unity C# code generator specializing in production-quality gameplay systems.

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

        AddCommand(CreateGenerateCommand());
        AddCommand(CreateIterateCommand());
        AddCommand(CreateListCommand());
    }

    private Command CreateGenerateCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var systemOpt = new Option<string>("--system", "Description of the gameplay system to generate.") { IsRequired = true };
        var outputDirOpt = new Option<string>("--output-dir", () => DefaultOutputDir, "Relative path under project-root for generated scripts.");
        var testDirOpt = new Option<string>("--test-dir", () => DefaultTestDir, "Relative path under project-root for generated tests.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Show what would be generated without writing files.");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("generate", "Generate a new Unity gameplay system.")
        {
            projectRootOpt, systemOpt, outputDirOpt, testDirOpt, dryRunOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var system = ctx.ParseResult.GetValueForOption(systemOpt)!;
            var outputDir = ctx.ParseResult.GetValueForOption(outputDirOpt) ?? DefaultOutputDir;
            var testDir = ctx.ParseResult.GetValueForOption(testDirOpt) ?? DefaultTestDir;
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteGenerateAsync(
                projectRoot, system, outputDir, testDir, dryRun, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateIterateCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var changeOpt = new Option<string>("--change", "Description of the change to make.") { IsRequired = true };
        var systemDirOpt = new Option<string>("--system-dir", "Relative folder under project-root containing the system to modify (e.g. Assets/Scripts/Weapons).") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("iterate", "Modify an existing Unity gameplay system.")
        {
            projectRootOpt, changeOpt, systemDirOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var change = ctx.ParseResult.GetValueForOption(changeOpt)!;
            var systemDir = ctx.ParseResult.GetValueForOption(systemDirOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteIterateAsync(
                projectRoot, change, systemDir, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateListCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("list", "List generated gameplay systems.")
        {
            projectRootOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = ExecuteList(projectRoot, json);
            await Task.CompletedTask;
        });

        return cmd;
    }

    internal async Task<int> ExecuteGenerateAsync(
        string projectRoot,
        string systemDescription,
        string outputDir,
        string testDir,
        bool dryRun,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var fullOutputDir = Path.Combine(fullProjectRoot, outputDir);
        var fullTestDir = Path.Combine(fullProjectRoot, testDir);

        SetPathAllowlist(fullProjectRoot, fullOutputDir, fullTestDir);

        var prompt = BuildGeneratePrompt(systemDescription, outputDir, testDir);

        if (dryRun)
        {
            WriteDryRunOutput(prompt, json);
            return 0;
        }

        var runner = _runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-generate", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            WriteError($"Generation failed: {result.Summary}", json);
            return 1;
        }

        var files = ParseFiles(result.Summary);
        var writtenFiles = new List<string>();

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(fullProjectRoot, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(fullPath, content, ct).ConfigureAwait(false);
            writtenFiles.Add(relativePath);
        }

        WriteManifest(fullOutputDir, systemDescription, writtenFiles);

        WriteGenerateResult(writtenFiles, systemDescription, outputDir, json);
        return 0;
    }

    internal async Task<int> ExecuteIterateAsync(
        string projectRoot,
        string changeDescription,
        string systemDir,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var fullSystemDir = Path.Combine(fullProjectRoot, systemDir);
        if (!Directory.Exists(fullSystemDir))
        {
            WriteError($"System directory not found: {fullSystemDir}", json);
            return 1;
        }

        var existingFiles = Directory.GetFiles(fullSystemDir, "*.cs", SearchOption.AllDirectories);
        if (existingFiles.Length == 0)
        {
            WriteError($"No .cs files found in {systemDir}", json);
            return 1;
        }

        SetPathAllowlist(fullProjectRoot, fullSystemDir);

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
        }

        var prompt = BuildIteratePrompt(changeDescription, systemDir, contextBuilder.ToString());

        var runner = _runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-iterate", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            WriteError($"Iteration failed: {result.Summary}", json);
            return 1;
        }

        var files = ParseFiles(result.Summary);
        var writtenFiles = new List<string>();

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(fullProjectRoot, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(fullPath, content, ct).ConfigureAwait(false);
            writtenFiles.Add(relativePath);
        }

        WriteManifest(fullSystemDir, changeDescription, writtenFiles);

        WriteIterateResult(writtenFiles, changeDescription, systemDir, json);
        return 0;
    }

    internal static int ExecuteList(string projectRoot, bool json)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var generatedRoot = Path.Combine(fullProjectRoot, DefaultOutputDir);
        if (!Directory.Exists(generatedRoot))
        {
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(new { ok = true, systems = Array.Empty<object>() }));
            else
                Console.WriteLine("No generated systems found.");
            return 0;
        }

        var systems = new List<object>();
        foreach (var dir in Directory.GetDirectories(generatedRoot))
        {
            var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
            if (csFiles.Length == 0)
                continue;

            var dirName = Path.GetFileName(dir);
            var manifestPath = Path.Combine(dir, ManifestFileName);
            string? prompt = null;
            string? generatedAt = null;

            if (File.Exists(manifestPath))
            {
                try
                {
                    var manifestJson = File.ReadAllText(manifestPath);
                    using var doc = JsonDocument.Parse(manifestJson);
                    if (doc.RootElement.TryGetProperty("prompt", out var p))
                        prompt = p.GetString();
                    if (doc.RootElement.TryGetProperty("generatedAt", out var g))
                        generatedAt = g.GetString();
                }
                catch
                {
                    // Ignore malformed manifest
                }
            }

            systems.Add(new
            {
                name = dirName,
                path = Path.GetRelativePath(fullProjectRoot, dir).Replace('\\', '/'),
                fileCount = csFiles.Length,
                prompt,
                generatedAt
            });
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok = true, systems }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            if (systems.Count == 0)
            {
                Console.WriteLine("No generated systems found.");
            }
            else
            {
                Console.WriteLine($"Generated systems ({systems.Count}):");
                foreach (dynamic sys in systems)
                    Console.WriteLine($"  {sys.name} ({sys.fileCount} files) - {sys.path}");
            }
        }

        return 0;
    }

    internal static bool ValidateProjectRoot(string fullProjectRoot, bool json)
    {
        if (!Directory.Exists(fullProjectRoot))
        {
            WriteError($"Project root not found: {fullProjectRoot}", json);
            return false;
        }

        var assetsDir = Path.Combine(fullProjectRoot, "Assets");
        if (!Directory.Exists(assetsDir))
        {
            WriteError($"Not a valid Unity project: missing Assets/ folder in {fullProjectRoot}", json);
            return false;
        }

        return true;
    }

    internal static List<(string RelativePath, string Content)> ParseFiles(string rawOutput)
    {
        var files = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(rawOutput))
            return files;

        var lines = rawOutput.Split('\n');
        string? currentPath = null;
        var currentContent = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith(FileMarkerPrefix, StringComparison.Ordinal))
            {
                if (currentPath != null)
                {
                    files.Add((currentPath, currentContent.ToString().TrimEnd()));
                    currentContent.Clear();
                }

                currentPath = line[FileMarkerPrefix.Length..].Trim().Replace('\\', '/');
            }
            else if (currentPath != null)
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentPath != null)
            files.Add((currentPath, currentContent.ToString().TrimEnd()));

        return files;
    }

    internal static void WriteManifest(string directory, string prompt, List<string> files)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var manifest = new
            {
                generatedAt = DateTime.UtcNow.ToString("O"),
                prompt,
                files
            };
            var manifestPath = Path.Combine(directory, ManifestFileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, options));
        }
        catch
        {
            // Best-effort manifest writing; don't fail the command
        }
    }

    private static string BuildGeneratePrompt(string systemDescription, string outputDir, string testDir)
    {
        return $@"{UnitySystemPrompt}

Generate a Unity gameplay system based on the following description:
{systemDescription}

Place script files under: {outputDir}/
Place test files under: {testDir}/
Each file must start with a // FILE: marker with the relative path from the project root.";
    }

    private static string BuildIteratePrompt(string changeDescription, string systemDir, string existingContext)
    {
        return $@"{UnitySystemPrompt}

You are modifying an existing Unity gameplay system located in: {systemDir}

{existingContext}

Requested change:
{changeDescription}

Output the complete modified files. Each file must start with a // FILE: marker with the relative path from the project root. Include all files that need changes.";
    }

    private static void SetPathAllowlist(string projectRoot, params string[] additionalPaths)
    {
        var existing = Environment.GetEnvironmentVariable("NEXO_PATH_ALLOWLIST_EXTRA") ?? "";
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(existing))
            paths.Add(existing);
        paths.Add(projectRoot);
        paths.AddRange(additionalPaths);
        Environment.SetEnvironmentVariable("NEXO_PATH_ALLOWLIST_EXTRA", string.Join(Path.PathSeparator.ToString(), paths));
    }

    private static void WriteError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = message }));
        else
            Console.Error.WriteLine($"unity-dev: error: {message}");
    }

    private static void WriteDryRunOutput(string prompt, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                dryRun = true,
                prompt
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine("=== DRY RUN ===");
            Console.WriteLine("The following prompt would be sent to the LLM:");
            Console.WriteLine();
            Console.WriteLine(prompt);
        }
    }

    private static void WriteGenerateResult(List<string> files, string systemDescription, string outputDir, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                action = "generate",
                system = systemDescription,
                outputDir,
                filesCreated = files
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev generate: ok");
            Console.WriteLine($"System: {systemDescription}");
            Console.WriteLine($"Files created ({files.Count}):");
            foreach (var f in files)
                Console.WriteLine($"  {f}");
        }
    }

    private static void WriteIterateResult(List<string> files, string changeDescription, string systemDir, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                action = "iterate",
                change = changeDescription,
                systemDir,
                filesModified = files
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev iterate: ok");
            Console.WriteLine($"Change: {changeDescription}");
            Console.WriteLine($"System: {systemDir}");
            Console.WriteLine($"Files modified ({files.Count}):");
            foreach (var f in files)
                Console.WriteLine($"  {f}");
        }
    }
}
