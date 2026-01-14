using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using System.Diagnostics;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Project adapter for web applications (React, Vue, Next.js, etc.).
/// </summary>
public class WebProjectAdapter : GenericProjectAdapter
{
    private readonly ILogger<WebProjectAdapter>? _logger;
    private string? _packageJsonPath;
    private string? _framework;
    
    public WebProjectAdapter(string projectPath, ILogger<WebProjectAdapter>? logger = null)
        : base(projectPath, logger)
    {
        _logger = logger;
    }
    
    public override Task InitializeAsync(CancellationToken ct = default)
    {
        // Find package.json
        _packageJsonPath = Path.Combine(_projectPath, "package.json");
        if (!File.Exists(_packageJsonPath))
        {
            _packageJsonPath = null;
        }
        else
        {
            // Detect framework
            var content = File.ReadAllText(_packageJsonPath);
            if (content.Contains("\"next\""))
                _framework = "Next.js";
            else if (content.Contains("\"react\""))
                _framework = "React";
            else if (content.Contains("\"vue\""))
                _framework = "Vue";
            else
                _framework = "Node.js";
        }
        
        return base.InitializeAsync(ct);
    }
    
    public override Task<ProjectContext> GetProjectContextAsync(CancellationToken ct = default)
    {
        var keyFiles = new List<string>();
        
        // Web-specific files
        var webPatterns = new[]
        {
            "package.json", "package-lock.json", "yarn.lock", "*.js", "*.jsx", "*.ts", "*.tsx",
            "*.vue", "*.html", "*.css", "*.scss", "next.config.js", "vite.config.js",
            "webpack.config.js", "tsconfig.json"
        };
        
        foreach (var pattern in webPatterns)
        {
            var files = Directory.GetFiles(_projectPath, pattern, SearchOption.TopDirectoryOnly)
                .Take(5)
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>();
            keyFiles.AddRange(files);
        }
        
        return Task.FromResult(new ProjectContext
        {
            ProjectType = _framework ?? "WebApp",
            PrimaryLanguage = "TypeScript",
            Framework = _framework,
            KeyFiles = keyFiles.Distinct().ToList(),
            ProjectPath = _projectPath
        });
    }
    
    public override async Task<BuildResult> BuildAsync(bool fullRebuild, CancellationToken ct = default)
    {
        if (_packageJsonPath == null)
        {
            return new BuildResult
            {
                Success = false,
                Errors = new[] { "No package.json file found" },
                Warnings = Array.Empty<string>(),
                OutputPath = null
            };
        }
        
        _logger?.LogInformation("Building web project: {Framework}", _framework);
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        try
        {
            // Determine build command based on framework
            string buildCommand;
            if (_framework == "Next.js")
            {
                buildCommand = "next build";
            }
            else
            {
                // Check package.json for build script
                var content = File.ReadAllText(_packageJsonPath);
                if (content.Contains("\"build\""))
                {
                    buildCommand = "npm run build";
                }
                else
                {
                    return new BuildResult
                    {
                        Success = false,
                        Errors = new[] { "No build script found in package.json" },
                        Warnings = Array.Empty<string>(),
                        OutputPath = null
                    };
                }
            }
            
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = buildCommand == "next build" ? "run build" : "run build",
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
                    if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("Error:", StringComparison.OrdinalIgnoreCase))
                        errors.Add(e.Data);
                    else if (e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                             e.Data.Contains("Warning:", StringComparison.OrdinalIgnoreCase))
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
                if (_framework == "Next.js")
                {
                    outputPath = Path.Combine(_projectPath, ".next");
                }
                else
                {
                    var distPath = Path.Combine(_projectPath, "dist");
                    var buildPath = Path.Combine(_projectPath, "build");
                    outputPath = Directory.Exists(distPath) ? distPath :
                                 Directory.Exists(buildPath) ? buildPath : null;
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
                Warnings = Array.Empty<string>(),
                OutputPath = null
            };
        }
    }
    
    public override Task<string> GetTestTargetAsync(CancellationToken ct = default)
    {
        // Web apps typically run on localhost
        if (_framework == "Next.js")
        {
            return Task.FromResult("http://localhost:3000");
        }
        
        // Check for dev server port in package.json or config
        if (_packageJsonPath != null)
        {
            try
            {
                var content = File.ReadAllText(_packageJsonPath);
                // Simple extraction - in production, use JSON parser
                var portMatch = System.Text.RegularExpressions.Regex.Match(
                    content,
                    @"""port"":\s*(\d+)");
                if (portMatch.Success)
                {
                    var port = portMatch.Groups[1].Value;
                    return Task.FromResult($"http://localhost:{port}");
                }
            }
            catch
            {
                // Fall through to default
            }
        }
        
        return Task.FromResult("http://localhost:3000");
    }
}
