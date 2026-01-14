using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using System.Diagnostics;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Project adapter for .NET applications and APIs.
/// </summary>
public class DotNetProjectAdapter : GenericProjectAdapter
{
    private readonly ILogger<DotNetProjectAdapter>? _logger;
    private string? _csprojPath;
    
    public DotNetProjectAdapter(string projectPath, ILogger<DotNetProjectAdapter>? logger = null)
        : base(projectPath, logger)
    {
        _logger = logger;
    }
    
    public override Task InitializeAsync(CancellationToken ct = default)
    {
        // Find .csproj file
        _csprojPath = Directory.GetFiles(_projectPath, "*.csproj", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        
        if (_csprojPath == null)
        {
            _logger?.LogWarning("No .csproj file found in {Path}", _projectPath);
        }
        
        return base.InitializeAsync(ct);
    }
    
    public override Task<ProjectContext> GetProjectContextAsync(CancellationToken ct = default)
    {
        var keyFiles = new List<string>();
        
        // .NET-specific files
        var dotnetPatterns = new[]
        {
            "*.csproj", "*.sln", "*.cs", "*.json", "appsettings.json", "Program.cs",
            "Startup.cs", "*.cshtml", "*.razor"
        };
        
        foreach (var pattern in dotnetPatterns)
        {
            var files = Directory.GetFiles(_projectPath, pattern, SearchOption.TopDirectoryOnly)
                .Take(5)
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>();
            keyFiles.AddRange(files);
        }
        
        // Determine if it's an API or app
        var isApi = _csprojPath != null && (
            File.ReadAllText(_csprojPath).Contains("Microsoft.AspNetCore.App") ||
            Directory.GetFiles(_projectPath, "Startup.cs", SearchOption.TopDirectoryOnly).Any() ||
            Directory.GetFiles(_projectPath, "Program.cs", SearchOption.TopDirectoryOnly)
                .Any(f => File.ReadAllText(f).Contains("WebApplication"))
        );
        
        return Task.FromResult(new ProjectContext
        {
            ProjectType = isApi ? "DotNetApi" : "DotNetApp",
            PrimaryLanguage = "C#",
            Framework = "ASP.NET Core",
            KeyFiles = keyFiles.Distinct().ToList(),
            ProjectPath = _projectPath
        });
    }
    
    public override async Task<BuildResult> BuildAsync(bool fullRebuild, CancellationToken ct = default)
    {
        if (_csprojPath == null)
        {
            return new BuildResult
            {
                Success = false,
                Errors = new[] { "No .csproj file found" },
                Warnings = Array.Empty<string>(),
                OutputPath = null
            };
        }
        
        _logger?.LogInformation("Building .NET project: {Project}", _csprojPath);
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        try
        {
            var buildArgs = fullRebuild ? "build --no-incremental" : "build";
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"{buildArgs} \"{_csprojPath}\"",
                    WorkingDirectory = _projectPath,
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
                    else if (e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                        warnings.Add(e.Data);
                }
            };
            
            buildProcess.Start();
            buildProcess.BeginErrorReadLine();
            
            var output = await buildProcess.StandardOutput.ReadToEndAsync(ct);
            await buildProcess.WaitForExitAsync(ct);
            
            var success = buildProcess.ExitCode == 0 && errors.Count == 0;
            
            // Find output path
            string? outputPath = null;
            if (success)
            {
                var binPath = Path.Combine(Path.GetDirectoryName(_csprojPath) ?? _projectPath, "bin");
                if (Directory.Exists(binPath))
                {
                    outputPath = Directory.GetDirectories(binPath)
                        .OrderByDescending(d => Directory.GetCreationTime(d))
                        .FirstOrDefault();
                }
            }
            
            return new BuildResult
            {
                Success = success,
                Errors = errors,
                Warnings = warnings,
                OutputPath = outputPath
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
        // For APIs, return the URL
        var isApi = GetProjectContextAsync(ct).Result.ProjectType == "DotNetApi";
        
        if (isApi)
        {
            // Try to read launchSettings.json for URL
            var launchSettings = Path.Combine(_projectPath, "Properties", "launchSettings.json");
            if (File.Exists(launchSettings))
            {
                try
                {
                    var content = File.ReadAllText(launchSettings);
                    // Simple extraction - in production, use JSON parser
                    var urlMatch = System.Text.RegularExpressions.Regex.Match(
                        content,
                        @"""applicationUrl"":\s*""([^""]+)""");
                    if (urlMatch.Success)
                    {
                        var urls = urlMatch.Groups[1].Value.Split(';');
                        return Task.FromResult(urls[0]);
                    }
                }
                catch
                {
                    // Fall through to default
                }
            }
            
            return Task.FromResult("https://localhost:5001");
        }
        
        // For apps, return the executable path
        var outputPath = Path.Combine(_projectPath, "bin", "Debug", "net8.0");
        var exePath = Directory.GetFiles(outputPath, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        
        return Task.FromResult(exePath ?? _projectPath);
    }
}
