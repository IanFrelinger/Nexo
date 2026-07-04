using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Nexo.CLI.Unity.Pipeline;
namespace Nexo.CLI.Commands.Unity;
/// <summary>Unity dev command utilities.</summary>
internal static class UnityDevCommandUtilities
{
    internal static int ExecuteList(string projectRoot, bool json)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!ValidateProjectRoot(fullProjectRoot, json))
            return 1;
        var generatedRoot = Path.Combine(fullProjectRoot, UnityDevCommand.DefaultOutputDir);
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
            var manifestPath = Path.Combine(dir, UnityDevCommand.ManifestFileName);
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
            Console.Out.Flush();
            Console.SetOut(TextWriter.Null);
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
            /// <summary>Write error.</summary>
            WriteError($"Project root not found: {fullProjectRoot}", json);
            return false;
        }
        var assetsDir = Path.Combine(fullProjectRoot, "Assets");
        if (!Directory.Exists(assetsDir))
        {
            /// <summary>Write error.</summary>
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

            if (line.StartsWith(UnityDevCommand.FileMarkerPrefix, StringComparison.Ordinal))
            {
                if (currentPath != null)
                {
                    files.Add((currentPath, currentContent.ToString().TrimEnd()));
                    currentContent.Clear();
                }

                currentPath = line[UnityDevCommand.FileMarkerPrefix.Length..].Trim().Replace('\\', '/');
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
            var manifestPath = Path.Combine(directory, UnityDevCommand.ManifestFileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, options));
        }
        catch
        {
            // Best-effort manifest writing; don't fail the command
        }
    }


    internal static string BuildGeneratePrompt(
        string systemDescription,
        string outputDir,
        string testDir,
        string constraintFragment = "",
        string templateFragment = "",
        string compositionContext = "")
    {
        return $@"{UnityDevCommand.UnitySystemPrompt}{constraintFragment}{templateFragment}

    Generate a Unity gameplay system based on the following description:
    {systemDescription}
    {(string.IsNullOrEmpty(compositionContext) ? "" : $"\nContext from upstream systems:\n{compositionContext}\n")}
    Place script files under: {outputDir}/
    Place test files under: {testDir}/
    Each file must start with a // FILE: marker with the relative path from the project root.";
    }


    internal static string BuildIteratePrompt(string changeDescription, string systemDir, string existingContext)
    {
        return $@"{UnityDevCommand.UnitySystemPrompt}

    You are modifying an existing Unity gameplay system located in: {systemDir}

    {existingContext}

    Requested change:
    {changeDescription}

    Output the complete modified files. Each file must start with a // FILE: marker with the relative path from the project root. Include all files that need changes.";
    }


    internal static void SetPathAllowlist(string projectRoot, params string[] additionalPaths)
    {
        var existing = Environment.GetEnvironmentVariable("NEXO_PATH_ALLOWLIST_EXTRA") ?? "";
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(existing))
            paths.Add(existing);

        // Add Unity-standard relative prefixes so the PathAllowlist policy
        // approves writes to Assets/, Packages/, ProjectSettings/ etc.
        paths.Add("Assets/");
        paths.Add("Packages/");
        paths.Add("ProjectSettings/");

        // Also add the full project root as SandboxRoot for absolute path resolution
        Environment.SetEnvironmentVariable("NEXO_SANDBOX_ROOT", projectRoot);
        Environment.SetEnvironmentVariable("NEXO_PATH_ALLOWLIST_EXTRA", string.Join(",", paths));
    }


    internal static void WriteError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = message }));
        else
            Console.Error.WriteLine($"unity-dev: error: {message}");
    }


    internal static void WriteDryRunOutput(string prompt, bool json)
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


    internal static void WriteGenerateResult(List<string> files, string systemDescription, string outputDir, bool json)
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


    internal static void WriteIterateResult(List<string> files, string changeDescription, string systemDir, bool json)
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


    internal static int ExecutePin(string projectRoot, string file, string field, string? value, bool unpin, bool json)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        var descriptorPath = Path.Combine(fullProjectRoot, file);

        if (unpin)
        {
            DescriptorOverrides.Unpin(descriptorPath, field);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "unpin", file, field }));
            else
                Console.WriteLine($"Unpinned '{field}' from {file}");
        }
        else
        {
            if (value == null)
            {
                /// <summary>Write error.</summary>
                WriteError("--value is required when pinning a field.", json);
                return 1;
            }
            DescriptorOverrides.Pin(descriptorPath, field, value);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "pin", file, field, value }));
            else
                Console.WriteLine($"Pinned '{field}' = '{value}' on {file}");
        }

        return 0;
    }


    /// <summary>Creates asset prompt.</summary>
    /// <param name="assetType">Asset type.</param>
    /// <param name="description">Description.</param>
    internal static string BuildAssetPrompt(string assetType, string description) => UnityAssetPromptBuilder.BuildAssetPrompt(assetType, description);


    internal static void WriteAssetsResult(List<string> files, string assetType, string description, bool json)
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


    internal static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "\n... (truncated)";
    }


}
