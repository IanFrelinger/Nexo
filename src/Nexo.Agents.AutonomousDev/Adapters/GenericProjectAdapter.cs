using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using System.Diagnostics;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Generic file-based project adapter.
/// </summary>
public class GenericProjectAdapter : IProjectAdapter
{
    protected readonly string _projectPath;
    private readonly ILogger<GenericProjectAdapter>? _logger;
    
    public GenericProjectAdapter(string projectPath, ILogger<GenericProjectAdapter>? logger = null)
    {
        _projectPath = projectPath;
        _logger = logger;
    }
    
    public virtual Task InitializeAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {_projectPath}");
        
        _logger?.LogInformation("Initialized project adapter for {Path}", _projectPath);
        return Task.CompletedTask;
    }
    
    public virtual Task<ProjectContext> GetProjectContextAsync(CancellationToken ct = default)
    {
        var keyFiles = new List<string>();
        
        // Find common project files
        var patterns = new[] { "*.csproj", "*.sln", "*.json", "*.cs", "*.ts", "*.js", "*.py" };
        foreach (var pattern in patterns)
        {
            keyFiles.AddRange(Directory.GetFiles(_projectPath, pattern, SearchOption.TopDirectoryOnly)
                .Take(5)
                .Select(Path.GetFileName)
                .Where(f => f != null)!);
        }
        
        return Task.FromResult(new ProjectContext
        {
            ProjectType = "Generic",
            PrimaryLanguage = InferLanguage(),
            KeyFiles = keyFiles.Distinct().ToList()
        });
    }
    
    public async Task<IReadOnlyList<CodeContext>> GetRelevantCodeAsync(string[] areas, CancellationToken ct = default)
    {
        var contexts = new List<CodeContext>();
        
        foreach (var area in areas)
        {
            // Try to find files matching the area
            var searchPath = Path.Combine(_projectPath, area);
            if (Directory.Exists(searchPath))
            {
                var files = Directory.GetFiles(searchPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsCodeFile(f))
                    .Take(10);
                
                foreach (var file in files)
                {
                    var content = await ReadFileAsync(file, ct);
                    if (content != null)
                    {
                        contexts.Add(new CodeContext
                        {
                            Path = Path.GetRelativePath(_projectPath, file),
                            Content = content,
                            Language = InferLanguageFromExtension(file)
                        });
                    }
                }
            }
        }
        
        return contexts;
    }
    
    public async Task<string?> ReadFileAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_projectPath, path);
        
        if (!File.Exists(fullPath))
            return null;
        
        return await File.ReadAllTextAsync(fullPath, ct);
    }
    
    public async Task WriteFileAsync(string path, string content, CancellationToken ct = default)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_projectPath, path);
        var dir = Path.GetDirectoryName(fullPath);
        
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        await File.WriteAllTextAsync(fullPath, content, ct);
        _logger?.LogInformation("Wrote file: {Path}", path);
    }
    
    public async Task PatchFileAsync(string path, string patch, CancellationToken ct = default)
    {
        // Simple implementation: treat patch as full file replacement
        // In production, would use proper diff/patch library
        await WriteFileAsync(path, patch, ct);
    }
    
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_projectPath, path);
        return Task.FromResult(File.Exists(fullPath));
    }
    
    public virtual async Task<BuildResult> BuildAsync(bool fullRebuild, CancellationToken ct = default)
    {
        // Try to detect build command
        var buildCommand = DetectBuildCommand();
        
        if (buildCommand == null)
        {
            return new BuildResult
            {
                Success = false,
                Errors = new[] { "Could not detect build command for this project type" },
                Duration = TimeSpan.Zero
            };
        }
        
        var (command, args) = buildCommand.Value;
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();
        
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = _projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            
            if (process == null)
            {
                return new BuildResult
                {
                    Success = false,
                    Errors = new[] { "Failed to start build process" },
                    Duration = sw.Elapsed
                };
            }
            
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            
            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                errors.AddRange(error.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            }
            
            // Parse warnings from output
            warnings.AddRange(output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains("warning", StringComparison.OrdinalIgnoreCase)));
            
            return new BuildResult
            {
                Success = process.ExitCode == 0,
                Errors = errors,
                Warnings = warnings,
                RawOutput = output + "\n" + error,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new BuildResult
            {
                Success = false,
                Errors = new[] { ex.Message },
                Duration = sw.Elapsed
            };
        }
    }
    
    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        var backupPath = Path.Combine(Path.GetDirectoryName(_projectPath)!, 
            $"{Path.GetFileName(_projectPath)}_backup_{DateTime.Now:yyyyMMdd_HHmmss}");
        
        // Simple copy - in production would use proper backup library
        await Task.Run(() =>
        {
            CopyDirectory(_projectPath, backupPath);
        }, ct);
        
        _logger?.LogInformation("Created backup at {Path}", backupPath);
        return backupPath;
    }
    
    public virtual Task<string> GetTestTargetAsync(CancellationToken ct = default)
    {
        // Try to infer from project type
        // For now, return a placeholder
        return Task.FromResult("http://localhost:5000"); // Default web target
    }
    
    public async Task CommitChangesAsync(string message, CancellationToken ct = default)
    {
        // Try git commit
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"commit -m \"{message}\"",
                WorkingDirectory = _projectPath,
                UseShellExecute = false
            });
            
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                _logger?.LogInformation("Committed changes: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to commit changes");
        }
    }
    
    private string InferLanguage()
    {
        if (Directory.GetFiles(_projectPath, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            return "C#";
        if (Directory.GetFiles(_projectPath, "package.json", SearchOption.TopDirectoryOnly).Any())
            return "JavaScript/TypeScript";
        if (Directory.GetFiles(_projectPath, "*.py", SearchOption.TopDirectoryOnly).Any())
            return "Python";
        return "Unknown";
    }
    
    private static string? InferLanguageFromExtension(string file)
    {
        return Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".json" => "json",
            ".xml" => "xml",
            _ => null
        };
    }
    
    private static bool IsCodeFile(string file)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        return ext is ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".c" or ".h";
    }
    
    private (string, string)? DetectBuildCommand()
    {
        if (Directory.GetFiles(_projectPath, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            return ("dotnet", "build");
        if (Directory.GetFiles(_projectPath, "package.json", SearchOption.TopDirectoryOnly).Any())
            return ("npm", "run build");
        if (Directory.GetFiles(_projectPath, "Makefile", SearchOption.TopDirectoryOnly).Any())
            return ("make", "");
        return null;
    }
    
    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        }
        
        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
