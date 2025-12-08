using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Build.Models;
using Nexo.Orchestration.Build.Ports;

namespace Nexo.Tools.Unity;

/// <summary>
/// Unity build tool implementation.
/// </summary>
public sealed class UnityBuildTool : IBuildTool
{
    private readonly ILogger<UnityBuildTool> _logger;
    private readonly string _unityPath;

    public string ToolName => "Unity";
    public IReadOnlyList<BuildTarget> SupportedTargets => new[]
    {
        BuildTarget.Windows,
        BuildTarget.MacOS,
        BuildTarget.Linux,
        BuildTarget.iOS,
        BuildTarget.Android,
        BuildTarget.WebGL
    };

    public UnityBuildTool(IConfiguration configuration, ILogger<UnityBuildTool> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unityPath = configuration["Unity:EditorPath"]
            ?? FindUnityEditor()
            ?? throw new InvalidOperationException("Unity editor not found");
    }

    public async Task<BuildOutput> BuildAsync(
        BuildConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var buildId = Guid.NewGuid().ToString();
        var outputPath = config.OutputPath ?? Path.Combine(config.ProjectPath, "Builds", config.Target.ToString());
        var logPath = Path.Combine(Path.GetTempPath(), $"unity_build_{buildId}.log");

        var args = new List<string>
        {
            "-batchmode",
            "-nographics",
            "-projectPath", config.ProjectPath,
            "-buildTarget", MapBuildTarget(config.Target),
            "-logFile", logPath,
            "-executeMethod", "Nexo.BuildScript.Build",
            $"-outputPath={outputPath}",
            $"-development={config.Development.ToString().ToLowerInvariant()}",
            "-quit"
        };

        if (config.Scenes.Any())
        {
            args.Add($"-scenes={string.Join(",", config.Scenes)}");
        }

        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting Unity build: {Target} at {ProjectPath}",
            config.Target, config.ProjectPath);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _unityPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        await process.WaitForExitAsync(cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        var success = process.ExitCode == 0;

        // Parse log file for errors and warnings
        var (errors, warnings) = await ParseBuildLogAsync(logPath, cancellationToken);

        // Gather metrics if successful
        BuildMetrics? metrics = null;
        if (success && Directory.Exists(outputPath))
        {
            metrics = GatherBuildMetrics(outputPath, config.ProjectPath);
        }

        _logger.LogInformation("Unity build completed: Success={Success}, Duration={Duration}",
            success, duration);

        return new BuildOutput
        {
            BuildId = buildId,
            Success = success,
            Target = config.Target,
            ArtifactPath = success ? outputPath : null,
            Duration = duration,
            Errors = errors,
            Warnings = warnings,
            Metrics = metrics
        };
    }

    private async Task<(IReadOnlyList<BuildError>, IReadOnlyList<BuildWarning>)> ParseBuildLogAsync(
        string logPath,
        CancellationToken cancellationToken)
    {
        var errors = new List<BuildError>();
        var warnings = new List<BuildWarning>();

        if (!File.Exists(logPath)) return (errors, warnings);

        var lines = await File.ReadAllLinesAsync(logPath, cancellationToken);

        foreach (var line in lines)
        {
            if (line.Contains("error CS") || line.Contains("Error:"))
            {
                errors.Add(new BuildError
                {
                    Code = ExtractErrorCode(line) ?? "UNITY001",
                    Message = line,
                    Severity = BuildErrorSeverity.Error
                });
            }
            else if (line.Contains("warning CS") || line.Contains("Warning:"))
            {
                warnings.Add(new BuildWarning
                {
                    Code = ExtractErrorCode(line) ?? "UNITY_WARN",
                    Message = line
                });
            }
        }

        return (errors, warnings);
    }

    private BuildMetrics GatherBuildMetrics(string outputPath, string projectPath)
    {
        var artifactSize = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);

        var scriptCount = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories).Length;
        var assetCount = Directory.GetFiles(Path.Combine(projectPath, "Assets"), "*", SearchOption.AllDirectories).Length;
        var shaderCount = Directory.GetFiles(projectPath, "*.shader", SearchOption.AllDirectories).Length;

        return new BuildMetrics
        {
            ArtifactSizeBytes = artifactSize,
            ScriptCount = scriptCount,
            AssetCount = assetCount,
            ShaderCount = shaderCount
        };
    }

    private string MapBuildTarget(BuildTarget target) => target switch
    {
        BuildTarget.Windows => "Win64",
        BuildTarget.MacOS => "OSXUniversal",
        BuildTarget.Linux => "Linux64",
        BuildTarget.iOS => "iOS",
        BuildTarget.Android => "Android",
        BuildTarget.WebGL => "WebGL",
        _ => throw new ArgumentException($"Unsupported build target: {target}")
    };

    private string? FindUnityEditor()
    {
        var paths = new[]
        {
            "/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity",
            "C:\\Program Files\\Unity\\Hub\\Editor\\*\\Editor\\Unity.exe",
            "/opt/unity/Editor/Unity"
        };

        foreach (var pattern in paths)
        {
            try
            {
                var dir = Path.GetDirectoryName(pattern);
                var file = Path.GetFileName(pattern);
                if (dir != null && Directory.Exists(dir))
                {
                    var matches = Directory.GetFiles(dir, file, SearchOption.AllDirectories);
                    if (matches.Any()) return matches.First();
                }
            }
            catch
            {
                // Continue searching
            }
        }

        return null;
    }

    private string? ExtractErrorCode(string line)
    {
        var match = Regex.Match(line, @"(CS\d{4}|error|warning)\s*:");
        return match.Success ? match.Groups[1].Value : null;
    }
}

