using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        AddCommand(CreateInitCommand());
        AddCommand(CreateGenerateCommand());
        AddCommand(CreateIterateCommand());
        AddCommand(CreateListCommand());
        AddCommand(CreateAssetsCommand());
        AddCommand(CreateQaCommand());
        AddCommand(CreateFullstackCommand());
    }

    private Command CreateInitCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path where the Unity project should be created.") { IsRequired = true };
        var nameOpt = new Option<string>("--name", () => "NexoForgeGame", "Project name (used for folder naming and Assembly Definitions).");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("init", "Scaffold a new Unity project structure ready for Nexo Forge development.")
        {
            projectRootOpt, nameOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var name = ctx.ParseResult.GetValueForOption(nameOpt) ?? "NexoForgeGame";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await Task.FromResult(ExecuteInit(projectRoot, name, json));
        });

        return cmd;
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

        // Auto-scaffold the project if it doesn't exist or is missing Assets/
        if (!Directory.Exists(fullProjectRoot) || !Directory.Exists(Path.Combine(fullProjectRoot, "Assets")))
        {
            var projectName = Path.GetFileName(fullProjectRoot);
            if (string.IsNullOrWhiteSpace(projectName)) projectName = "NexoForgeGame";

            if (!json) Console.WriteLine($"Project not found at {fullProjectRoot} — scaffolding new Unity project '{projectName}'...");
            var initResult = ExecuteInit(projectRoot, projectName, json);
            if (initResult != 0) return initResult;
        }

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

    internal static int ExecuteInit(string projectRoot, string projectName, bool json)
    {
        var fullPath = Path.GetFullPath(projectRoot);
        Directory.CreateDirectory(fullPath);

        var dirs = new[]
        {
            "Assets/Scenes",
            "Assets/Scripts",
            "Assets/Scripts/Generated",
            "Assets/Prefabs",
            "Assets/Materials",
            "Assets/ScriptableObjects",
            "Assets/Tests/EditMode",
            "Assets/Tests/EditMode/Generated",
            "Assets/Tests/PlayMode",
            "Packages",
            "ProjectSettings",
            ".nexo"
        };

        foreach (var dir in dirs)
            Directory.CreateDirectory(Path.Combine(fullPath, dir));

        var manifestPath = Path.Combine(fullPath, "Packages", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
            {
                dependencies = new Dictionary<string, string>
                {
                    ["com.unity.inputsystem"] = "1.7.0",
                    ["com.unity.textmeshpro"] = "3.0.6",
                    ["com.unity.test-framework"] = "1.3.9",
                    ["com.unity.netcode.gameobjects"] = "1.8.1"
                }
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        var projectSettingsPath = Path.Combine(fullPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(projectSettingsPath))
            File.WriteAllText(projectSettingsPath, "m_EditorVersion: 2022.3.0f1\n");

        var gitignorePath = Path.Combine(fullPath, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, @"# Unity
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
*.csproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.unityproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Nexo
.nexo/*.db
.nexo/preferences.json
");
        }

        var asmdefPath = Path.Combine(fullPath, "Assets", "Scripts", $"{projectName}.asmdef");
        if (!File.Exists(asmdefPath))
        {
            File.WriteAllText(asmdefPath, JsonSerializer.Serialize(new
            {
                name = projectName,
                rootNamespace = projectName,
                references = new[] { "Unity.InputSystem", "Unity.Netcode.Runtime" },
                includePlatforms = Array.Empty<string>(),
                excludePlatforms = Array.Empty<string>(),
                allowUnsafeCode = false,
                overrideReferences = false,
                precompiledReferences = Array.Empty<string>(),
                autoReferenced = true,
                defineConstraints = Array.Empty<string>(),
                versionDefines = Array.Empty<string>(),
                noEngineReferences = false
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        var testAsmdefPath = Path.Combine(fullPath, "Assets", "Tests", "EditMode", $"{projectName}.Tests.asmdef");
        if (!File.Exists(testAsmdefPath))
        {
            File.WriteAllText(testAsmdefPath, JsonSerializer.Serialize(new
            {
                name = $"{projectName}.Tests",
                rootNamespace = $"{projectName}.Tests",
                references = new[] { projectName, "UnityEngine.TestRunner", "UnityEditor.TestRunner" },
                includePlatforms = new[] { "Editor" },
                excludePlatforms = Array.Empty<string>(),
                allowUnsafeCode = false,
                overrideReferences = true,
                precompiledReferences = new[] { "nunit.framework.dll" },
                autoReferenced = false,
                defineConstraints = new[] { "UNITY_INCLUDE_TESTS" },
                versionDefines = Array.Empty<string>(),
                noEngineReferences = false
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        var nexoConfigPath = Path.Combine(fullPath, ".nexo", "config.json");
        if (!File.Exists(nexoConfigPath))
        {
            File.WriteAllText(nexoConfigPath, JsonSerializer.Serialize(new
            {
                provider = "ollama",
                model = "llama3.1"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        var createdFiles = new List<string>();
        foreach (var dir in dirs) createdFiles.Add(dir + "/");
        if (File.Exists(manifestPath)) createdFiles.Add("Packages/manifest.json");
        if (File.Exists(asmdefPath)) createdFiles.Add($"Assets/Scripts/{projectName}.asmdef");
        if (File.Exists(testAsmdefPath)) createdFiles.Add($"Assets/Tests/EditMode/{projectName}.Tests.asmdef");
        createdFiles.Add(".gitignore");
        createdFiles.Add(".nexo/config.json");
        createdFiles.Add("ProjectSettings/ProjectVersion.txt");

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                projectRoot = fullPath,
                projectName,
                filesCreated = createdFiles
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"Unity project scaffolded at: {fullPath}");
            Console.WriteLine($"  Project name: {projectName}");
            Console.WriteLine($"  Directories: {dirs.Length}");
            Console.WriteLine($"  Package manifest with InputSystem, Netcode, TestFramework");
            Console.WriteLine($"  Assembly definitions for scripts and tests");
            Console.WriteLine($"  .gitignore for Unity + Nexo");
            Console.WriteLine($"  .nexo/config.json (default: Ollama provider)");
            Console.WriteLine();
            Console.WriteLine("Next: open this folder in Unity Hub, or run:");
            Console.WriteLine($"  nexo unity-dev generate --project-root {projectRoot} --system \"your system description\"");
        }

        return 0;
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

    // ───────────────────────────────────────────────────────────────────
    //  assets subcommand
    // ───────────────────────────────────────────────────────────────────

    private Command CreateAssetsCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var typeOpt = new Option<string>("--type", "Asset type to generate (material|prefab|scene|audio|ui).") { IsRequired = true };
        var descriptionOpt = new Option<string>("--description", "Description of the asset to generate.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("assets", "Generate game asset definitions (JSON descriptors and C# loaders).")
        {
            projectRootOpt, typeOpt, descriptionOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var type = ctx.ParseResult.GetValueForOption(typeOpt)!;
            var description = ctx.ParseResult.GetValueForOption(descriptionOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteAssetsAsync(
                projectRoot, type, description, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    internal async Task<int> ExecuteAssetsAsync(
        string projectRoot,
        string assetType,
        string description,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var validTypes = new[] { "material", "prefab", "scene", "audio", "ui" };
        var normalizedType = assetType.ToLowerInvariant();
        if (!validTypes.Contains(normalizedType))
        {
            WriteError($"Invalid asset type '{assetType}'. Must be one of: {string.Join(", ", validTypes)}", json);
            return 1;
        }

        var assetDir = Path.Combine(fullProjectRoot, "Assets", "NexoAssets", normalizedType);
        Directory.CreateDirectory(assetDir);

        SetPathAllowlist(fullProjectRoot, assetDir);

        var prompt = BuildAssetPrompt(normalizedType, description);

        var runner = _runnerFactory();
        var result = await runner.RunAsync(fullProjectRoot, prompt, "unity-dev-assets", ct).ConfigureAwait(false);

        if (!result.Success)
        {
            WriteError($"Asset generation failed: {result.Summary}", json);
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

        WriteManifest(assetDir, description, writtenFiles);
        WriteAssetsResult(writtenFiles, normalizedType, description, json);
        return 0;
    }

    internal static string BuildAssetPrompt(string assetType, string description)
    {
        var schemaHint = assetType switch
        {
            "material" => "MaterialDescriptor JSON with fields: Id, Name, ShaderName, Color (hex), Metallic (0-1), Smoothness (0-1), EmissionColor (hex or null), RenderMode (Opaque|Cutout|Transparent), TextureSlots (dict of slot name to texture path).",
            "prefab" => "PrefabDescriptor JSON with fields: Id, Name, Components (list of {TypeName, Properties}), Children (nested PrefabDescriptors), Position/Rotation/Scale (Vector3 with X,Y,Z).",
            "scene" => "SceneDescriptor JSON with fields: Id, Name, RootObjects (list of PrefabDescriptors), AmbientLightColor (hex), SkyboxMaterial (string or null), NavMeshAreas (list of {Name, Center, Size} with Vector3).",
            "audio" => "AudioDescriptor JSON with fields: Id, Name, Category (sfx|music|ambient|ui), Volume (0-1), Pitch, SpatialBlend (0-1), MinDistance, MaxDistance, Loop (bool).",
            "ui" => "UIDescriptor JSON with fields: Id, Name, CanvasMode (overlay|camera|worldspace), Elements (list of {Type (text|button|image|panel|slider|input), Name, Position (Vector3), Size (Vector2 with X,Y), Properties}).",
            _ => ""
        };

        return $@"{UnitySystemPrompt}

You are generating a Unity asset descriptor and its companion C# loader script.

Asset type: {assetType}
Description: {description}

Schema: {schemaHint}

Generate TWO files:
1. A JSON descriptor file at: Assets/NexoAssets/{assetType}/{{name}}.json
   - The JSON must conform exactly to the schema above.
2. A C# loader script at: Assets/NexoAssets/{assetType}/{{name}}Loader.cs
   - The loader should read the JSON at runtime using JsonUtility or System.Text.Json and create the corresponding Unity objects.
   - Use a MonoBehaviour that loads on Awake or a static utility method.

Each file must start with a // FILE: marker with the relative path from the project root.";
    }

    private static void WriteAssetsResult(List<string> files, string assetType, string description, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                action = "assets",
                assetType,
                description,
                filesCreated = files
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev assets: ok");
            Console.WriteLine($"Type: {assetType}");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Files created ({files.Count}):");
            foreach (var f in files)
                Console.WriteLine($"  {f}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    //  qa subcommand
    // ───────────────────────────────────────────────────────────────────

    private Command CreateQaCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var maxIterOpt = new Option<int>("--max-iterations", () => 5, "Maximum compile/test/fix iterations.");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");
        var unityPathOpt = new Option<string?>("--unity-path", () => null, "Path to the Unity editor executable. Auto-detected if omitted.");

        var cmd = new Command("qa", "Automated compile, test, and iterative-fix loop using the Unity editor CLI.")
        {
            projectRootOpt, maxIterOpt, jsonOpt, unityPathOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var maxIter = ctx.ParseResult.GetValueForOption(maxIterOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var unityPath = ctx.ParseResult.GetValueForOption(unityPathOpt);

            ctx.ExitCode = await ExecuteQaAsync(
                projectRoot, maxIter, json, unityPath,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    internal async Task<int> ExecuteQaAsync(
        string projectRoot,
        int maxIterations,
        bool json,
        string? unityPath,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var unity = unityPath ?? FindUnityEditor();
        if (unity == null)
        {
            WriteError("Unity editor not found. Provide --unity-path or ensure Unity is on PATH.", json);
            return 1;
        }

        SetPathAllowlist(fullProjectRoot);
        var iterations = new List<object>();

        for (int i = 1; i <= maxIterations; i++)
        {
            if (!json) Console.WriteLine($"QA iteration {i}/{maxIterations}: compiling...");

            var (buildExit, buildOutput) = await RunUnityCommand(
                unity, fullProjectRoot,
                "-batchmode -nographics -logFile - -quit",
                ct).ConfigureAwait(false);

            if (buildExit != 0)
            {
                if (!json) Console.WriteLine($"  Compilation failed (exit {buildExit}). Requesting LLM fix...");

                var fixPrompt = $@"{UnitySystemPrompt}

The Unity project at {fullProjectRoot} failed to compile.
Build output:
{Truncate(buildOutput, 4000)}

Analyse the errors and generate corrected files. Each file must start with a // FILE: marker.";

                var runner = _runnerFactory();
                var fixResult = await runner.RunAsync(fullProjectRoot, fixPrompt, "unity-dev-qa-fix", ct).ConfigureAwait(false);

                var fixedFiles = ParseFiles(fixResult.Summary);
                foreach (var (rp, content) in fixedFiles)
                {
                    var fp = Path.Combine(fullProjectRoot, rp);
                    var d = Path.GetDirectoryName(fp);
                    if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d);
                    await File.WriteAllTextAsync(fp, content, ct).ConfigureAwait(false);
                }

                iterations.Add(new { iteration = i, phase = "build", passed = false, filesFixed = fixedFiles.Count });
                continue;
            }

            if (!json) Console.WriteLine("  Compilation succeeded. Running EditMode tests...");

            var testResultPath = Path.Combine(fullProjectRoot, "TestResults", $"qa-iter-{i}.xml");
            var testDir = Path.GetDirectoryName(testResultPath);
            if (!string.IsNullOrEmpty(testDir)) Directory.CreateDirectory(testDir);

            var (testExit, testOutput) = await RunUnityCommand(
                unity, fullProjectRoot,
                $"-batchmode -nographics -runTests -testPlatform EditMode -testResults \"{testResultPath}\" -logFile - -quit",
                ct).ConfigureAwait(false);

            if (testExit == 0)
            {
                if (!json) Console.WriteLine("  All tests passed!");
                iterations.Add(new { iteration = i, phase = "test", passed = true, filesFixed = 0 });
                WriteQaResult(iterations, i, true, json);
                return 0;
            }

            if (!json) Console.WriteLine($"  Tests failed (exit {testExit}). Requesting LLM fix...");

            var testFixPrompt = $@"{UnitySystemPrompt}

The Unity project at {fullProjectRoot} compiled successfully but EditMode tests failed.
Test output:
{Truncate(testOutput, 4000)}

Analyse the test failures and generate corrected files. Each file must start with a // FILE: marker.";

            var testRunner = _runnerFactory();
            var testFixResult = await testRunner.RunAsync(fullProjectRoot, testFixPrompt, "unity-dev-qa-fix", ct).ConfigureAwait(false);

            var testFixedFiles = ParseFiles(testFixResult.Summary);
            foreach (var (rp, content) in testFixedFiles)
            {
                var fp = Path.Combine(fullProjectRoot, rp);
                var d = Path.GetDirectoryName(fp);
                if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d);
                await File.WriteAllTextAsync(fp, content, ct).ConfigureAwait(false);
            }

            iterations.Add(new { iteration = i, phase = "test", passed = false, filesFixed = testFixedFiles.Count });
        }

        if (!json) Console.WriteLine($"QA: max iterations ({maxIterations}) reached without all tests passing.");
        WriteQaResult(iterations, maxIterations, false, json);
        return 1;
    }

    internal static async Task<(int exitCode, string output)> RunUnityCommand(
        string unityPath, string projectRoot, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = unityPath,
            Arguments = $"-projectPath \"{projectRoot}\" {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return (process.ExitCode, outputBuilder.ToString());
    }

    internal static string? FindUnityEditor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var hubBase = "/Applications/Unity/Hub/Editor";
            if (Directory.Exists(hubBase))
            {
                foreach (var dir in Directory.GetDirectories(hubBase).OrderByDescending(d => d))
                {
                    var candidate = Path.Combine(dir, "Unity.app", "Contents", "MacOS", "Unity");
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hubBase = @"C:\Program Files\Unity\Hub\Editor";
            if (Directory.Exists(hubBase))
            {
                foreach (var dir in Directory.GetDirectories(hubBase).OrderByDescending(d => d))
                {
                    var candidate = Path.Combine(dir, "Editor", "Unity.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which",
                Arguments = "unity",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var path = proc.StandardOutput.ReadLine()?.Trim();
                proc.WaitForExit();
                if (proc.ExitCode == 0 && !string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
        }
        catch
        {
            // which/where not available; fall through
        }

        return null;
    }

    private static void WriteQaResult(List<object> iterations, int totalIterations, bool allPassed, bool json)
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

    internal static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "\n... (truncated)";
    }

    // ───────────────────────────────────────────────────────────────────
    //  fullstack subcommand
    // ───────────────────────────────────────────────────────────────────

    private Command CreateFullstackCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var gameDescOpt = new Option<string>("--game-description", "High-level description of the game to build.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("fullstack", "Run the full pipeline: init → generate → assets → qa.")
        {
            projectRootOpt, gameDescOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var gameDesc = ctx.ParseResult.GetValueForOption(gameDescOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteFullstackAsync(
                projectRoot, gameDesc, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    internal async Task<int> ExecuteFullstackAsync(
        string projectRoot,
        string gameDescription,
        bool json,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        var steps = new List<object>();

        // Step 1: init
        if (!json) Console.WriteLine("fullstack: step 1/4 — init");
        var initCode = ExecuteInit(projectRoot, Path.GetFileName(fullProjectRoot) ?? "NexoForgeGame", json);
        steps.Add(new { step = "init", exitCode = initCode });
        if (initCode != 0) { WriteFullstackResult(steps, false, json); return initCode; }

        // Step 2: generate core systems
        if (!json) Console.WriteLine("fullstack: step 2/4 — generate");
        var genCode = await ExecuteGenerateAsync(
            projectRoot, gameDescription, DefaultOutputDir, DefaultTestDir,
            false, json, ct).ConfigureAwait(false);
        steps.Add(new { step = "generate", exitCode = genCode });
        if (genCode != 0) { WriteFullstackResult(steps, false, json); return genCode; }

        // Step 3: generate material + prefab assets
        if (!json) Console.WriteLine("fullstack: step 3/4 — assets");
        var matCode = await ExecuteAssetsAsync(
            projectRoot, "material", $"Materials for: {gameDescription}", json, ct).ConfigureAwait(false);
        steps.Add(new { step = "assets-material", exitCode = matCode });

        var prefabCode = await ExecuteAssetsAsync(
            projectRoot, "prefab", $"Prefabs for: {gameDescription}", json, ct).ConfigureAwait(false);
        steps.Add(new { step = "assets-prefab", exitCode = prefabCode });

        // Step 4: qa loop
        if (!json) Console.WriteLine("fullstack: step 4/4 — qa");
        var qaCode = await ExecuteQaAsync(projectRoot, 5, json, null, ct).ConfigureAwait(false);
        steps.Add(new { step = "qa", exitCode = qaCode });

        var allOk = initCode == 0 && genCode == 0 && qaCode == 0;
        WriteFullstackResult(steps, allOk, json);
        return allOk ? 0 : 1;
    }

    private static void WriteFullstackResult(List<object> steps, bool success, bool json)
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
        }
    }
}
