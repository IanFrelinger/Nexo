using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.UnitySidecarDemo;

internal static class Program
{
    private const string DefaultOutputRoot = "tools/unity-demo-output";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var options = ParseOptions(args.Skip(1).ToArray());
        var repoRoot = DiscoverRepoRoot();
        if (repoRoot == null)
        {
            Console.Error.WriteLine("Could not locate repo root (Nexo.sln not found).");
            return 1;
        }

        return command switch
        {
            "generate" => await GenerateAsync(repoRoot, options),
            "validate" => await ValidateAsync(repoRoot, options),
            "run-demo" => await RunDemoAsync(repoRoot, options),
            "supervise" => await SuperviseAsync(repoRoot, options),
            _ => UnknownCommand(command)
        };
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static async Task<int> RunDemoAsync(string repoRoot, Dictionary<string, string> options)
    {
        if (!options.ContainsKey("prompt"))
            options["prompt"] = "add a dash ability with cooldown and stamina cost";

        var generateExit = await GenerateAsync(repoRoot, options);
        if (generateExit != 0)
            return generateExit;

        var validateExit = await ValidateAsync(repoRoot, options);
        if (validateExit != 0)
            return validateExit;

        var dogfoodResult = await RunCommandCaptureAsync(
            "dotnet",
            "run --project src/Nexo.CLI --no-build -- dogfood block1 --format-json",
            repoRoot);

        var payload = new
        {
            ok = dogfoodResult.ExitCode == 0,
            stage = "dogfood-block1",
            exitCode = dogfoodResult.ExitCode,
            output = dogfoodResult.StdOut.Trim(),
            error = dogfoodResult.StdErr.Trim()
        };
        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions()));

        return dogfoodResult.ExitCode;
    }

    private static async Task<int> SuperviseAsync(string repoRoot, Dictionary<string, string> options)
    {
        var gameConcept = options.TryGetValue("game", out var requestedGame) && !string.IsNullOrWhiteSpace(requestedGame)
            ? requestedGame.Trim()
            : "a co-op arena action roguelite";

        var outputRoot = options.TryGetValue("output-root", out var providedRoot)
            ? providedRoot
            : DefaultOutputRoot;

        var iterations = 2;
        if (options.TryGetValue("iterations", out var rawIterations))
        {
            if (!int.TryParse(rawIterations, out iterations) || iterations <= 0)
            {
                Console.Error.WriteLine("supervise requires --iterations to be a positive integer.");
                return 2;
            }
        }

        var team = BuildSupervisorTeam();
        var allIterationsPassed = true;
        var iterationReports = new List<object>();
        var generatedFiles = new List<string>();

        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            var roleReports = new List<object>();
            foreach (var role in team)
            {
                var iterationPrompt = BuildSupervisorPrompt(gameConcept, role, iteration, iterations);
                var systemName = SanitizeClassName($"{role.RoleName}Iteration{iteration}System");
                var generateOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["prompt"] = iterationPrompt,
                    ["output-root"] = outputRoot,
                    ["system-name"] = systemName
                };

                var generateExit = await GenerateAsync(repoRoot, generateOptions);
                var generatedFile = Path.Combine(outputRoot, "Assets", "GeneratedSystems", $"{systemName}.cs");
                generatedFiles.Add(generatedFile);

                roleReports.Add(new
                {
                    role = role.RoleName,
                    focus = role.FocusArea,
                    systemName,
                    generatedFile,
                    exitCode = generateExit,
                    ok = generateExit == 0
                });

                if (generateExit != 0)
                {
                    allIterationsPassed = false;
                    iterationReports.Add(new
                    {
                        iteration,
                        ok = false,
                        roleReports
                    });
                    break;
                }
            }

            if (!allIterationsPassed)
                break;

            var validateExit = await ValidateAsync(repoRoot, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["output-root"] = outputRoot
            });

            var iterationPassed = validateExit == 0;
            if (!iterationPassed)
                allIterationsPassed = false;

            iterationReports.Add(new
            {
                iteration,
                ok = iterationPassed,
                validateExitCode = validateExit,
                roleReports
            });

            if (!iterationPassed)
                break;
        }

        var absoluteOutputRoot = Path.GetFullPath(Path.Combine(repoRoot, outputRoot));
        Directory.CreateDirectory(absoluteOutputRoot);
        var reportPath = Path.Combine(absoluteOutputRoot, "supervisor_report.json");
        var reportPayload = new
        {
            ok = allIterationsPassed,
            stage = "supervise",
            game = gameConcept,
            iterations,
            generatedFiles,
            iterationReports,
            generatedAtUtc = DateTimeOffset.UtcNow
        };
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(reportPayload, JsonOptions()));

        var consolePayload = new
        {
            ok = allIterationsPassed,
            stage = "supervise",
            game = gameConcept,
            iterations,
            teamSize = team.Count,
            outputRoot = Path.GetRelativePath(repoRoot, absoluteOutputRoot),
            reportFile = Path.GetRelativePath(repoRoot, reportPath)
        };
        Console.WriteLine(JsonSerializer.Serialize(consolePayload, JsonOptions()));
        return allIterationsPassed ? 0 : 1;
    }

    private static async Task<int> GenerateAsync(string repoRoot, Dictionary<string, string> options)
    {
        if (!options.TryGetValue("prompt", out var prompt) || string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("generate requires --prompt \"...\"");
            return 2;
        }

        var outputRoot = options.TryGetValue("output-root", out var providedRoot)
            ? providedRoot
            : DefaultOutputRoot;
        var absoluteOutputRoot = Path.GetFullPath(Path.Combine(repoRoot, outputRoot));

        Directory.CreateDirectory(absoluteOutputRoot);
        var assetsRoot = Path.Combine(absoluteOutputRoot, "Assets");
        var generatedSystemsRoot = Path.Combine(assetsRoot, "GeneratedSystems");
        var contractsRoot = Path.Combine(generatedSystemsRoot, "Contracts");
        Directory.CreateDirectory(generatedSystemsRoot);
        Directory.CreateDirectory(contractsRoot);

        var nexoResult = await RunCommandCaptureAsync(
            "dotnet",
            $"run --project src/Nexo.CLI --no-build -- chat --prompt \"{EscapeForShell(prompt)}\"",
            repoRoot,
            env: new Dictionary<string, string> { ["NEXO_ALLOW_MOCK"] = "1" });

        var className = options.TryGetValue("system-name", out var providedName)
            ? SanitizeClassName(providedName)
            : PromptToClassName(prompt);

        var contracts = BuildContractsSource();
        await File.WriteAllTextAsync(Path.Combine(contractsRoot, "IGeneratedGameplaySystem.cs"), contracts.InterfaceCode);
        await File.WriteAllTextAsync(Path.Combine(contractsRoot, "SystemContext.cs"), contracts.ContextCode);

        var generatedCode = BuildGeneratedSystemCode(className, prompt);
        var generatedPath = Path.Combine(generatedSystemsRoot, $"{className}.cs");
        await File.WriteAllTextAsync(generatedPath, generatedCode);

        var manifest = new
        {
            prompt,
            className,
            generatedPath = Path.GetRelativePath(repoRoot, generatedPath),
            nexoChatExitCode = nexoResult.ExitCode,
            nexoChatSummary = Truncate(nexoResult.StdOut.Trim(), 600)
        };
        var manifestPath = Path.Combine(absoluteOutputRoot, "generation_manifest.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions()));

        var payload = new
        {
            ok = true,
            stage = "generate",
            prompt,
            className,
            outputRoot = Path.GetRelativePath(repoRoot, absoluteOutputRoot),
            generatedFile = Path.GetRelativePath(repoRoot, generatedPath),
            manifestFile = Path.GetRelativePath(repoRoot, manifestPath),
            nexoChatExitCode = nexoResult.ExitCode
        };
        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions()));
        return 0;
    }

    private static async Task<int> ValidateAsync(string repoRoot, Dictionary<string, string> options)
    {
        var outputRoot = options.TryGetValue("output-root", out var providedRoot)
            ? providedRoot
            : DefaultOutputRoot;
        var absoluteOutputRoot = Path.GetFullPath(Path.Combine(repoRoot, outputRoot));
        var generatedSystemsRoot = Path.Combine(absoluteOutputRoot, "Assets", "GeneratedSystems");
        if (!Directory.Exists(generatedSystemsRoot))
        {
            Console.Error.WriteLine($"No generated systems found at: {generatedSystemsRoot}");
            return 1;
        }

        var tempValidationRoot = Path.Combine(absoluteOutputRoot, ".validation");
        if (Directory.Exists(tempValidationRoot))
            Directory.Delete(tempValidationRoot, true);
        Directory.CreateDirectory(tempValidationRoot);

        var csprojPath = Path.Combine(tempValidationRoot, "Validation.csproj");
        var programPath = Path.Combine(tempValidationRoot, "Program.cs");
        var unityStubPath = Path.Combine(tempValidationRoot, "UnityStubs.cs");
        var generatedCopyPath = Path.Combine(tempValidationRoot, "Generated");
        Directory.CreateDirectory(generatedCopyPath);

        foreach (var sourceFile in Directory.GetFiles(generatedSystemsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(generatedCopyPath, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destination, overwrite: true);
        }

        await File.WriteAllTextAsync(programPath, "Console.WriteLine(\"validation-ok\");");
        await File.WriteAllTextAsync(unityStubPath, UnityStubsCode());
        await File.WriteAllTextAsync(csprojPath, ValidationProjectFile());

        var buildResult = await RunCommandCaptureAsync(
            "dotnet",
            $"build \"{csprojPath}\" -v minimal",
            repoRoot);

        var payload = new
        {
            ok = buildResult.ExitCode == 0,
            stage = "validate",
            outputRoot = Path.GetRelativePath(repoRoot, absoluteOutputRoot),
            exitCode = buildResult.ExitCode,
            buildOutput = Truncate(buildResult.StdOut.Trim(), 1200),
            buildError = Truncate(buildResult.StdErr.Trim(), 1200)
        };
        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions()));

        return buildResult.ExitCode;
    }

    private static (string InterfaceCode, string ContextCode) BuildContractsSource()
    {
        const string interfaceCode = """
namespace Generated.Systems.Contracts;

public interface IGeneratedGameplaySystem
{
    string SystemId { get; }

    void Initialize(SystemContext context);

    void Tick(float deltaTime);

    void Teardown();
}
""";

        const string contextCode = """
using UnityEngine;

namespace Generated.Systems.Contracts;

public sealed class SystemContext
{
    public GameObject PlayerRoot { get; init; } = null!;

    public Transform PlayerTransform { get; init; } = null!;

    public float CurrentTime { get; init; }
}
""";

        return (interfaceCode, contextCode);
    }

    private static string BuildGeneratedSystemCode(string className, string prompt)
    {
        var dash = prompt.Contains("dash", StringComparison.OrdinalIgnoreCase);
        var health = prompt.Contains("health", StringComparison.OrdinalIgnoreCase) || prompt.Contains("pickup", StringComparison.OrdinalIgnoreCase);

        if (dash)
        {
            return $$"""
using Generated.Systems.Contracts;
using UnityEngine;

namespace Generated.Systems;

public sealed class {{className}} : MonoBehaviour, IGeneratedGameplaySystem
{
    [SerializeField] private float dashDistance = 6f;
    [SerializeField] private float cooldownSeconds = 1.2f;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float staminaCost = 20f;

    private SystemContext? _context;
    private float _nextDashTime;
    private float _stamina = 100f;

    public string SystemId => "{{className}}";

    public void Initialize(SystemContext context)
    {
        _context = context;
        _nextDashTime = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (_context == null)
            return;

        _stamina = Mathf.Min(100f, _stamina + deltaTime * 8f);
        if (Input.GetKeyDown(dashKey) && Time.time >= _nextDashTime && _stamina >= staminaCost)
        {
            _stamina -= staminaCost;
            _nextDashTime = Time.time + cooldownSeconds;
            _context.PlayerTransform.position += _context.PlayerTransform.forward * dashDistance;
        }
    }

    public void Teardown()
    {
        _context = null;
    }
}
""";
        }

        if (health)
        {
            return $$"""
using Generated.Systems.Contracts;
using UnityEngine;

namespace Generated.Systems;

public sealed class {{className}} : MonoBehaviour, IGeneratedGameplaySystem
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int pickupValue = 25;

    private SystemContext? _context;
    private int _currentHealth;

    public string SystemId => "{{className}}";

    public void Initialize(SystemContext context)
    {
        _context = context;
        _currentHealth = maxHealth;
    }

    public void Tick(float deltaTime)
    {
        _ = deltaTime;
    }

    public void ApplyPickup()
    {
        _currentHealth = _currentHealth + pickupValue;
        if (_currentHealth > maxHealth)
            _currentHealth = maxHealth;
    }

    public void Teardown()
    {
        _context = null;
    }
}
""";
        }

        return $$"""
using Generated.Systems.Contracts;
using UnityEngine;

namespace Generated.Systems;

public sealed class {{className}} : MonoBehaviour, IGeneratedGameplaySystem
{
    private SystemContext? _context;

    public string SystemId => "{{className}}";

    public void Initialize(SystemContext context)
    {
        _context = context;
    }

    public void Tick(float deltaTime)
    {
        _ = deltaTime;
    }

    public void Teardown()
    {
        _context = null;
    }
}
""";
    }

    private static string PromptToClassName(string prompt)
    {
        var words = Regex.Matches(prompt, "[A-Za-z0-9]+")
            .Select(m => m.Value)
            .Where(w => w.Length > 1)
            .Take(5)
            .Select(ToPascal)
            .ToList();

        var root = words.Count == 0 ? "GeneratedSystem" : string.Concat(words);
        if (!root.EndsWith("System", StringComparison.Ordinal))
            root += "System";

        return SanitizeClassName(root);
    }

    private static string SanitizeClassName(string candidate)
    {
        var cleaned = Regex.Replace(candidate, "[^A-Za-z0-9_]", string.Empty);
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "GeneratedSystem";
        if (!char.IsLetter(cleaned[0]) && cleaned[0] != '_')
            cleaned = "_" + cleaned;
        return cleaned;
    }

    private static string ToPascal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lower = value.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static string ValidationProjectFile()
    {
        return """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Generated/*.cs" />
    <Compile Include="Program.cs" />
    <Compile Include="UnityStubs.cs" />
  </ItemGroup>
</Project>
""";
    }

    private static string UnityStubsCode()
    {
        return """
namespace UnityEngine;

public class MonoBehaviour { }

public sealed class SerializeField : System.Attribute { }

public sealed class GameObject { }

public sealed class Transform
{
    public Vector3 position;
    public Vector3 forward = new(0f, 0f, 1f);
}

public struct Vector3
{
    public float x;
    public float y;
    public float z;

    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3 operator *(Vector3 v, float m) => new(v.x * m, v.y * m, v.z * m);
}

public enum KeyCode
{
    LeftShift
}

public static class Input
{
    public static bool GetKeyDown(KeyCode key) => false;
}

public static class Time
{
    public static float time => 0f;
}

public static class Mathf
{
    public static float Min(float a, float b) => a < b ? a : b;
}
""";
    }

    private static async Task<CommandResult> RunCommandCaptureAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        if (env != null)
        {
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var process = Process.Start(psi);
        if (process == null)
            return new CommandResult(1, string.Empty, "Failed to start process.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var stdout = await stdOutTask;
        var stderr = await stdErrTask;

        return new CommandResult(process.ExitCode, stdout, stderr);
    }

    private static string EscapeForShell(string value)
    {
        return value.Replace("\"", "\\\"");
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions { WriteIndented = true };
    }

    private static bool IsHelp(string arg)
    {
        return arg is "-h" or "--help" or "help";
    }

    private static string? DiscoverRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(current);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                continue;

            var key = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = args[i + 1];
                i++;
            }
            else
            {
                options[key] = "true";
            }
        }
        return options;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }

    private static void PrintHelp()
    {
        var help = new StringBuilder();
        help.AppendLine("Nexo.UnitySidecarDemo");
        help.AppendLine();
        help.AppendLine("Commands:");
        help.AppendLine("  generate  --prompt \"...\" [--output-root tools/unity-demo-output] [--system-name DashAbilitySystem]");
        help.AppendLine("  validate  [--output-root tools/unity-demo-output]");
        help.AppendLine("  run-demo  [--prompt \"...\"] [--output-root tools/unity-demo-output]");
        help.AppendLine("  supervise [--game \"...\"] [--iterations 2] [--output-root tools/unity-demo-output]");
        help.AppendLine();
        help.AppendLine("Examples:");
        help.AppendLine("  dotnet run --project tools/Nexo.UnitySidecarDemo -- generate --prompt \"add a dash ability\"");
        help.AppendLine("  dotnet run --project tools/Nexo.UnitySidecarDemo -- validate");
        help.AppendLine("  dotnet run --project tools/Nexo.UnitySidecarDemo -- run-demo --prompt \"add a health pickup system\"");
        help.AppendLine("  dotnet run --project tools/Nexo.UnitySidecarDemo -- supervise --game \"build a coop dungeon crawler\" --iterations 2");
        Console.WriteLine(help.ToString());
    }

    private static List<SupervisorRole> BuildSupervisorTeam()
    {
        return new List<SupervisorRole>
        {
            new("Gameplay", "Core movement and player loop", "Design or improve the core movement loop for {GAME}. Include a dash or traversal interaction and clear tunables."),
            new("Combat", "Combat interactions and balance", "Design combat mechanics for {GAME}. Include timing windows, risk/reward, and a dash-attack interaction."),
            new("Economy", "Rewards and progression pacing", "Design rewards and progression for {GAME}. Include health pickup balance, recovery pacing, and spend/sink signals."),
            new("AI", "Enemy and encounter behavior", "Design enemy behavior for {GAME}. Define one encounter pattern, telegraphs, and adaptation to player movement.")
        };
    }

    private static string BuildSupervisorPrompt(
        string gameConcept,
        SupervisorRole role,
        int iteration,
        int totalIterations)
    {
        return $"{role.PromptTemplate.Replace("{GAME}", gameConcept, StringComparison.Ordinal)} " +
               $"Iteration {iteration} of {totalIterations}: produce a focused, implementable gameplay system script.";
    }
}

internal sealed record CommandResult(int ExitCode, string StdOut, string StdErr);
internal sealed record SupervisorRole(string RoleName, string FocusArea, string PromptTemplate);
