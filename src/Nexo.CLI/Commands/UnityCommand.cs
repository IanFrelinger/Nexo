using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command handler for Unity-related operations.
/// </summary>
public class UnityCommand
{
    private readonly ILogger<UnityCommand> _logger;

    public UnityCommand(ILogger<UnityCommand> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates the Unity command group with all subcommands.
    /// </summary>
    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var unityCmd = new Command("unity", "Unity Editor operations");

        var logger = serviceProvider.GetRequiredService<ILogger<UnityCommand>>();
        var command = new UnityCommand(logger);

        // nexo unity create
        var createCmd = new Command("create", "Create a new Unity project");
        createCmd.AddArgument(new Argument<string>("name", "Project name"));
        var createPathOpt = new Option<DirectoryInfo?>("--path", "Project path (defaults to current directory)");
        var createUnityBinOpt = new Option<string?>("--unity-bin", "Path to Unity executable (or set UNITY_BIN env var)");
        createCmd.AddOption(createPathOpt);
        createCmd.AddOption(createUnityBinOpt);

        createCmd.SetHandler(async (string name, DirectoryInfo? path, string? unityBin, bool json, bool verbose) =>
        {
            var exitCode = await command.CreateProjectAsync(name, path, unityBin, json, verbose);
            Environment.Exit(exitCode);
        }, createCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
           createPathOpt,
           createUnityBinOpt,
           jsonOpt,
           verboseOpt);

        // nexo unity open
        var openCmd = new Command("open", "Open Unity Editor for a project");
        openCmd.AddArgument(new Argument<DirectoryInfo>("project", "Path to Unity project"));
        var openUnityBinOpt = new Option<string?>("--unity-bin", "Path to Unity executable (or set UNITY_BIN env var)");
        openCmd.AddOption(openUnityBinOpt);

        openCmd.SetHandler(async (DirectoryInfo project, string? unityBin, bool json, bool verbose) =>
        {
            var exitCode = await command.OpenEditorAsync(project, unityBin, json, verbose);
            Environment.Exit(exitCode);
        }, openCmd.Arguments[0] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           openUnityBinOpt,
           jsonOpt,
           verboseOpt);

        // nexo unity run
        var runCmd = new Command("run", "Run Unity Editor method in batchmode");
        runCmd.AddArgument(new Argument<DirectoryInfo>("project", "Path to Unity project"));
        runCmd.AddArgument(new Argument<string>("method", "Editor method to execute"));
        var runUnityBinOpt = new Option<string?>("--unity-bin", "Path to Unity executable (or set UNITY_BIN env var)");
        var runArgsOpt = new Option<string[]>("--args", "Additional arguments to pass to Unity");
        var runLogFileOpt = new Option<FileInfo?>("--log-file", "Path to Unity log file");
        runCmd.AddOption(runUnityBinOpt);
        runCmd.AddOption(runArgsOpt);
        runCmd.AddOption(runLogFileOpt);

        runCmd.SetHandler(async (DirectoryInfo project, string method, string? unityBin, string[]? args, FileInfo? logFile, bool json, bool verbose) =>
        {
            var exitCode = await command.RunMethodAsync(project, method, unityBin, args ?? Array.Empty<string>(), logFile, json, verbose);
            Environment.Exit(exitCode);
        }, runCmd.Arguments[0] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           runCmd.Arguments[1] as Argument<string> ?? throw new InvalidOperationException(),
           runUnityBinOpt,
           runArgsOpt,
           runLogFileOpt,
           jsonOpt,
           verboseOpt);

        // nexo unity logs
        var logsCmd = new Command("logs", "Analyze Unity log files");
        logsCmd.AddArgument(new Argument<FileInfo>("log-file", "Path to Unity log file"));
        var logsOutputOpt = new Option<FileInfo?>("--output", "Output JSON analysis file");
        logsCmd.AddOption(logsOutputOpt);

        logsCmd.SetHandler(async (FileInfo logFile, FileInfo? output, bool json, bool verbose) =>
        {
            var exitCode = await command.AnalyzeLogsAsync(logFile, output, json, verbose);
            Environment.Exit(exitCode);
        }, logsCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           logsOutputOpt,
           jsonOpt,
           verboseOpt);

        unityCmd.AddCommand(createCmd);
        unityCmd.AddCommand(openCmd);
        unityCmd.AddCommand(runCmd);
        unityCmd.AddCommand(logsCmd);

        return unityCmd;
    }

    public async Task<int> CreateProjectAsync(string name, DirectoryInfo? path, string? unityBin, bool json, bool verbose)
    {
        try
        {
            var projectPath = path ?? new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, name));
            var unityPath = FindUnityExecutable(unityBin);

            if (unityPath == null)
            {
                _logger.LogError("Unity executable not found. Set UNITY_BIN environment variable or use --unity-bin option");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
                }
                return 1;
            }

            if (projectPath.Exists && projectPath.GetFiles("ProjectSettings/ProjectSettings.asset").Any())
            {
                _logger.LogInformation("Unity project already exists at {Path}", projectPath.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { success = true, message = "Project already exists", path = projectPath.FullName }));
                }
                return 0;
            }

            _logger.LogInformation("Creating Unity project: {Name} at {Path}", name, projectPath.FullName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityPath,
                    Arguments = $"-batchmode -nographics -createProject \"{projectPath.FullName}\" -quit",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && projectPath.Exists)
            {
                _logger.LogInformation("Unity project created successfully at {Path}", projectPath.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { success = true, path = projectPath.FullName }));
                }
                return 0;
            }
            else
            {
                _logger.LogError("Failed to create Unity project. Exit code: {ExitCode}", process.ExitCode);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Failed to create project", exitCode = process.ExitCode }));
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Unity project");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return 1;
        }
    }

    public Task<int> OpenEditorAsync(DirectoryInfo project, string? unityBin, bool json, bool verbose)
    {
        try
        {
            if (!project.Exists)
            {
                _logger.LogError("Unity project not found at {Path}", project.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Project not found", path = project.FullName }));
                }
                return Task.FromResult(1);
            }

            var unityPath = FindUnityExecutable(unityBin);
            if (unityPath == null)
            {
                _logger.LogError("Unity executable not found");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
                }
                return Task.FromResult(1);
            }

            _logger.LogInformation("Opening Unity Editor for project: {Path}", project.FullName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityPath,
                    Arguments = $"-projectPath \"{project.FullName}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };

            process.Start();

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { success = true, path = project.FullName }));
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Unity Editor");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return Task.FromResult(1);
        }
    }

    public async Task<int> RunMethodAsync(DirectoryInfo project, string method, string? unityBin, string[] args, FileInfo? logFile, bool json, bool verbose)
    {
        try
        {
            if (!project.Exists)
            {
                _logger.LogError("Unity project not found at {Path}", project.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Project not found", path = project.FullName }));
                }
                return 1;
            }

            var unityPath = FindUnityExecutable(unityBin);
            if (unityPath == null)
            {
                _logger.LogError("Unity executable not found");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
                }
                return 1;
            }

            var logPath = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"unity_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
            var logsDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            var unityArgs = new List<string>
            {
                "-batchmode",
                "-nographics",
                "-projectPath", $"\"{project.FullName}\"",
                "-executeMethod", method,
                "-logFile", $"\"{logPath}\"",
                "-quit"
            };

            unityArgs.AddRange(args);

            _logger.LogInformation("Running Unity method: {Method} in project: {Path}", method, project.FullName);
            if (verbose)
            {
                _logger.LogDebug("Unity command: {UnityPath} {Args}", unityPath, string.Join(" ", unityArgs));
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityPath,
                    Arguments = string.Join(" ", unityArgs),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            // Analyze logs if log file exists
            if (File.Exists(logPath))
            {
                var analysisFile = logFile != null 
                    ? Path.ChangeExtension(logFile.FullName, ".analysis.json")
                    : Path.ChangeExtension(logPath, ".analysis.json");

                _ = await AnalyzeLogsAsync(new FileInfo(logPath), new FileInfo(analysisFile), json, verbose);
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new 
                { 
                    success = process.ExitCode == 0, 
                    exitCode = process.ExitCode,
                    logFile = logPath
                }));
            }

            return process.ExitCode == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Unity method");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return 1;
        }
    }

    public async Task<int> AnalyzeLogsAsync(FileInfo logFile, FileInfo? output, bool json, bool verbose)
    {
        try
        {
            if (!logFile.Exists)
            {
                _logger.LogError("Log file not found: {Path}", logFile.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Log file not found", path = logFile.FullName }));
                }
                return 1;
            }

            var errors = new List<string>();
            var warnings = new List<string>();
            var info = new List<string>();

            var lines = await File.ReadAllLinesAsync(logFile.FullName);
            foreach (var line in lines)
            {
                if (line.Contains("Aborting batchmode", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Exception:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Error:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("could not be found", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(line);
                }
                else if (line.Contains("Warning:", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("⚠", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add(line);
                }
                else if ((line.Contains("Info:", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("INFO", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("✅", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("📋", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("🎮", StringComparison.OrdinalIgnoreCase)) &&
                         !line.Contains("Unity", StringComparison.OrdinalIgnoreCase) &&
                         !line.Contains("Editor", StringComparison.OrdinalIgnoreCase) &&
                         !line.Contains("AssetDatabase", StringComparison.OrdinalIgnoreCase))
                {
                    info.Add(line);
                }
            }

            var analysis = new
            {
                logFile = logFile.FullName,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                errors = errors,
                warnings = warnings,
                info = info,
                errorCount = errors.Count,
                warningCount = warnings.Count,
                infoCount = info.Count,
                hasErrors = errors.Count > 0,
                hasWarnings = warnings.Count > 0,
                summary = errors.Count > 0
                    ? $"Found {errors.Count} error(s) and {warnings.Count} warning(s)"
                    : warnings.Count > 0
                        ? $"Found {warnings.Count} warning(s), no errors"
                        : "No errors or warnings found"
            };

            var outputPath = output?.FullName ?? Path.ChangeExtension(logFile.FullName, ".analysis.json");
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true }));

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(analysis));
            }
            else
            {
                _logger.LogInformation("Log Analysis Results:");
                _logger.LogInformation("  Errors: {ErrorCount}", errors.Count);
                _logger.LogInformation("  Warnings: {WarningCount}", warnings.Count);
                
                if (errors.Count > 0)
                {
                    _logger.LogWarning("Errors found:");
                    foreach (var err in errors.Take(5))
                    {
                        _logger.LogWarning("  - {Error}", err);
                    }
                }

                if (warnings.Count > 0)
                {
                    _logger.LogWarning("Warnings found:");
                    foreach (var warn in warnings.Take(5))
                    {
                        _logger.LogWarning("  - {Warning}", warn);
                    }
                }

                _logger.LogInformation("Analysis saved to: {Path}", outputPath);
            }

            return errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Unity logs");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return 1;
        }
    }

    private string? FindUnityExecutable(string? explicitPath)
    {
        if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        var envPath = Environment.GetEnvironmentVariable("UNITY_BIN");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Try macOS paths
        var macHubPath = "/Applications/Unity/Hub/Editor";
        if (Directory.Exists(macHubPath))
        {
            var path = FindLatestUnityInHub(macHubPath, "Unity.app/Contents/MacOS/Unity");
            if (path != null) return path;
        }

        // Try Windows paths
        var windowsHubPaths = new[]
        {
            @"C:\Program Files\Unity\Hub\Editor",
            @"C:\Program Files (x86)\Unity\Hub\Editor",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor")
        };

        foreach (var hubPath in windowsHubPaths)
        {
            if (Directory.Exists(hubPath))
            {
                var path = FindLatestUnityInHub(hubPath, @"Editor\Unity.exe");
                if (path != null) return path;
            }
        }

        // Try Linux paths
        var linuxPaths = new[]
        {
            "/opt/unity/Editor/Unity",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity/Hub/Editor")
        };

        foreach (var linuxPath in linuxPaths)
        {
            if (File.Exists(linuxPath))
            {
                return linuxPath;
            }

            if (Directory.Exists(linuxPath))
            {
                var path = FindLatestUnityInHub(linuxPath, "Unity");
                if (path != null) return path;
            }
        }

        return null;
    }

    private string? FindLatestUnityInHub(string hubPath, string relativeExecutablePath)
    {
        try
        {
            var versionDirs = Directory.GetDirectories(hubPath)
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.Name)
                .ToList();

            foreach (var versionDir in versionDirs)
            {
                var executablePath = Path.Combine(versionDir.FullName, relativeExecutablePath);
                if (File.Exists(executablePath))
                {
                    _logger.LogInformation("Found Unity {Version} at {Path}", versionDir.Name, executablePath);
                    return executablePath;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching Unity Hub path {Path}", hubPath);
        }

        return null;
    }
}
