using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using System.Diagnostics;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Project adapter for Unity game projects.
/// </summary>
public class UnityProjectAdapter : GenericProjectAdapter
{
    private readonly ILogger<UnityProjectAdapter>? _logger;
    
    public UnityProjectAdapter(string projectPath, ILogger<UnityProjectAdapter>? logger = null)
        : base(projectPath, logger)
    {
        _logger = logger;
    }
    
    public override Task<ProjectContext> GetProjectContextAsync(CancellationToken ct = default)
    {
        var keyFiles = new List<string>();
        
        // Unity-specific files
        var unityPatterns = new[]
        {
            "*.cs", "*.unity", "*.prefab", "*.asset", "*.mat", "*.controller",
            "ProjectSettings/ProjectSettings.asset", "Assets/**/*.cs"
        };
        
        foreach (var pattern in unityPatterns)
        {
            if (pattern.Contains("**"))
            {
                var dir = pattern.Split('/')[0];
                var searchPath = Path.Combine(_projectPath, dir);
                if (Directory.Exists(searchPath))
                {
                    var files = Directory.GetFiles(searchPath, "*.cs", SearchOption.AllDirectories)
                        .Take(10)
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>();
                    keyFiles.AddRange(files);
                }
            }
            else
            {
                    var files = Directory.GetFiles(_projectPath, pattern, SearchOption.TopDirectoryOnly)
                        .Take(5)
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>();
                    keyFiles.AddRange(files);
            }
        }
        
        return Task.FromResult(new ProjectContext
        {
            ProjectType = "UnityGame",
            PrimaryLanguage = "C#",
            Framework = "Unity",
            KeyFiles = keyFiles.Distinct().ToList(),
            ProjectPath = _projectPath
        });
    }
    
    public override async Task<BuildResult> BuildAsync(bool fullRebuild, CancellationToken ct = default)
    {
        // Unity builds require Unity Editor or Unity CLI
        // For now, we'll try to compile C# scripts if possible
        _logger?.LogInformation("Building Unity project (limited support)");
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Check for Unity installation
        var unityPath = FindUnityEditor();
        if (unityPath == null)
        {
            errors.Add("Unity Editor not found. Unity builds require Unity Editor or Unity CLI.");
            return new BuildResult
            {
                Success = false,
                Errors = errors,
                Warnings = warnings,
                OutputPath = null
            };
        }
        
        // Try to use Unity CLI if available
        try
        {
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityPath,
                    Arguments = $"-batchmode -quit -projectPath \"{_projectPath}\" -buildTarget StandaloneWindows64 -buildPath \"{Path.Combine(_projectPath, "Build")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            buildProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase))
                        errors.Add(e.Data);
                    else
                        warnings.Add(e.Data);
                }
            };
            
            buildProcess.Start();
            buildProcess.BeginErrorReadLine();
            
            var output = await buildProcess.StandardOutput.ReadToEndAsync(ct);
            await buildProcess.WaitForExitAsync(ct);
            
            var success = buildProcess.ExitCode == 0 && errors.Count == 0;
            
            return new BuildResult
            {
                Success = success,
                Errors = errors,
                Warnings = warnings,
                OutputPath = success ? Path.Combine(_projectPath, "Build") : null
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Build failed: {ex.Message}");
            return new BuildResult
            {
                Success = false,
                Errors = errors,
                Warnings = warnings,
                OutputPath = null
            };
        }
    }
    
    public override Task<string> GetTestTargetAsync(CancellationToken ct = default)
    {
        // Unity games typically run as executables
        var buildPath = Path.Combine(_projectPath, "Build");
        var exePath = Directory.GetFiles(buildPath, "*.exe", SearchOption.AllDirectories)
            .FirstOrDefault();
        
        if (exePath != null)
        {
            return Task.FromResult(exePath);
        }
        
        // Fallback: return path to Unity project (for editor testing)
        return Task.FromResult(_projectPath);
    }
    
    private static string? FindUnityEditor()
    {
        // Common Unity installation paths
        var commonPaths = new[]
        {
            @"C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe",
            @"C:\Program Files (x86)\Unity\Editor\Unity.exe",
            @"/Applications/Unity/Unity.app/Contents/MacOS/Unity",
            @"/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity"
        };
        
        foreach (var pattern in commonPaths)
        {
            if (pattern.Contains('*'))
            {
                var dir = Path.GetDirectoryName(pattern);
                if (dir != null && Directory.Exists(dir))
                {
                    var matches = Directory.GetDirectories(Path.GetDirectoryName(dir) ?? "", Path.GetFileName(dir) ?? "");
                    foreach (var match in matches)
                    {
                        var fullPath = Path.Combine(match, pattern.Split('*')[1].TrimStart('\\', '/'));
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                }
            }
            else if (File.Exists(pattern))
            {
                return pattern;
            }
        }
        
        return null;
    }
    
    // Use protected _projectPath from base class
}
