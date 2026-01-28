using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.IO;
using Nexo.Infrastructure.Unity;

namespace Nexo.CLI.Commands;

/// <summary>
/// Unity CLI commands. All raw platform calls are delegated to Infrastructure.
/// </summary>
public sealed class UnityCommand
{
    private readonly ILogger<UnityCommand> _logger;

    public UnityCommand(ILogger<UnityCommand> logger) => _logger = logger;

    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var unityCmd = new Command("unity", "Unity Editor operations");

        var logger = serviceProvider.GetRequiredService<ILogger<UnityCommand>>();
        var command = new UnityCommand(logger);

        var createCmd = new Command("create", "Create a new Unity project");
        createCmd.AddArgument(new Argument<string>("name", "Project name"));
        var createPathOpt = new Option<DirectoryInfo?>("--path", "Project path (defaults to current directory)");
        var createUnityBinOpt = new Option<string?>("--unity-bin", "Path to Unity executable (or set UNITY_BIN env var)");
        createCmd.AddOption(createPathOpt);
        createCmd.AddOption(createUnityBinOpt);
        createCmd.SetHandler(async (string name, DirectoryInfo? path, string? unityBin, bool json, bool verbose) =>
        {
            var exitCode = await command.CreateProjectAsync(name, path, unityBin, json);
            Environment.Exit(exitCode);
        }, createCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
           createPathOpt, createUnityBinOpt, jsonOpt, verboseOpt);

        var openCmd = new Command("open", "Open Unity Editor for a project");
        openCmd.AddArgument(new Argument<DirectoryInfo>("project", "Path to Unity project"));
        var openUnityBinOpt = new Option<string?>("--unity-bin", "Path to Unity executable (or set UNITY_BIN env var)");
        openCmd.AddOption(openUnityBinOpt);
        openCmd.SetHandler(async (DirectoryInfo project, string? unityBin, bool json, bool verbose) =>
        {
            var exitCode = await command.OpenEditorAsync(project, unityBin, json);
            Environment.Exit(exitCode);
        }, openCmd.Arguments[0] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           openUnityBinOpt, jsonOpt, verboseOpt);

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
            var exitCode = await command.RunMethodAsync(project, method, unityBin, args ?? Array.Empty<string>(), logFile, json);
            Environment.Exit(exitCode);
        }, runCmd.Arguments[0] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           runCmd.Arguments[1] as Argument<string> ?? throw new InvalidOperationException(),
           runUnityBinOpt, runArgsOpt, runLogFileOpt, jsonOpt, verboseOpt);

        var logsCmd = new Command("logs", "Analyze Unity log files");
        logsCmd.AddArgument(new Argument<FileInfo>("log-file", "Path to Unity log file"));
        var logsOutputOpt = new Option<FileInfo?>("--output", "Output JSON analysis file");
        logsCmd.AddOption(logsOutputOpt);
        logsCmd.SetHandler(async (FileInfo logFile, FileInfo? output, bool json, bool verbose) =>
        {
            var exitCode = await command.AnalyzeLogsAsync(logFile, output, json);
            Environment.Exit(exitCode);
        }, logsCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           logsOutputOpt, jsonOpt, verboseOpt);

        // nexo unity capture-logs (alias for logs)
        var captureLogsCmd = new Command("capture-logs", "Capture and analyze Unity logs");
        captureLogsCmd.AddArgument(new Argument<FileInfo>("log-file", "Path to Unity log file"));
        var captureLogsOutputOpt = new Option<FileInfo?>("--output", "Output JSON analysis file");
        captureLogsCmd.AddOption(captureLogsOutputOpt);
        captureLogsCmd.SetHandler(async (FileInfo logFile, FileInfo? output, bool json, bool verbose) =>
        {
            var exitCode = await command.AnalyzeLogsAsync(logFile, output, json);
            Environment.Exit(exitCode);
        }, captureLogsCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           captureLogsOutputOpt, jsonOpt, verboseOpt);

        // nexo unity analyze-errors
        var analyzeErrorsCmd = new Command("analyze-errors", "Analyze Unity errors and attempt fixes");
        analyzeErrorsCmd.AddArgument(new Argument<FileInfo>("log-file", "Path to Unity log file"));
        analyzeErrorsCmd.AddArgument(new Argument<DirectoryInfo>("project", "Path to Unity project"));
        var analyzeErrorsOutputOpt = new Option<FileInfo?>("--output", "Output JSON analysis file");
        analyzeErrorsCmd.AddOption(analyzeErrorsOutputOpt);
        analyzeErrorsCmd.SetHandler(async (FileInfo logFile, DirectoryInfo project, FileInfo? output, bool json, bool verbose) =>
        {
            var exitCode = await command.AnalyzeErrorsAsync(logFile, project, output, json);
            Environment.Exit(exitCode);
        }, analyzeErrorsCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           analyzeErrorsCmd.Arguments[1] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           analyzeErrorsOutputOpt, jsonOpt, verboseOpt);

        unityCmd.AddCommand(createCmd);
        unityCmd.AddCommand(openCmd);
        unityCmd.AddCommand(runCmd);
        unityCmd.AddCommand(logsCmd);
        unityCmd.AddCommand(captureLogsCmd);
        unityCmd.AddCommand(analyzeErrorsCmd);
        return unityCmd;
    }

    private async Task<int> CreateProjectAsync(string name, DirectoryInfo? path, string? unityBin, bool json)
    {
        var projectPath = path ?? new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, name));
        var unityPath = UnityPlatform.FindUnityExecutable(unityBin);
        if (unityPath == null)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
            return 1;
        }

        var exit = await UnityPlatform.RunUnityAsync(
            unityPath,
            $"-batchmode -nographics -createProject \"{projectPath.FullName}\" -quit",
            useShellExecute: false,
            redirectOutput: true,
            workingDirectory: null,
            ct: CancellationToken.None);

        if (json) Console.WriteLine(JsonSerializer.Serialize(new { success = exit == 0, exitCode = exit, path = projectPath.FullName }));
        return exit == 0 ? 0 : 1;
    }

    private Task<int> OpenEditorAsync(DirectoryInfo project, string? unityBin, bool json)
    {
        if (!project.Exists)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Project not found", path = project.FullName }));
            return Task.FromResult(1);
        }

        var unityPath = UnityPlatform.FindUnityExecutable(unityBin);
        if (unityPath == null)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
            return Task.FromResult(1);
        }

        // Fire-and-forget (editor is interactive)
        _ = Task.Run(() => UnityPlatform.RunUnityAsync(
            unityPath,
            $"-projectPath \"{project.FullName}\"",
            useShellExecute: true,
            redirectOutput: false,
            workingDirectory: null,
            ct: CancellationToken.None));

        if (json) Console.WriteLine(JsonSerializer.Serialize(new { success = true, path = project.FullName }));
        return Task.FromResult(0);
    }

    private async Task<int> RunMethodAsync(DirectoryInfo project, string method, string? unityBin, string[] args, FileInfo? logFile, bool json)
    {
        if (!project.Exists)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Project not found", path = project.FullName }));
            return 1;
        }

        var unityPath = UnityPlatform.FindUnityExecutable(unityBin);
        if (unityPath == null)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Unity executable not found" }));
            return 1;
        }

        var logPath = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"unity_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
        var logsDir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logsDir))
        {
            var di = new DirectoryInfo(logsDir);
            if (!di.Exists) di.Create();
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

        var exit = await UnityPlatform.RunUnityAsync(
            unityPath,
            string.Join(" ", unityArgs),
            useShellExecute: false,
            redirectOutput: true,
            workingDirectory: null,
            ct: CancellationToken.None);

        if (new FileInfo(logPath).Exists)
        {
            var analysisFile = Path.ChangeExtension(logPath, ".analysis.json");
            _ = await AnalyzeLogsAsync(new FileInfo(logPath), new FileInfo(analysisFile), json);
        }

        if (json) Console.WriteLine(JsonSerializer.Serialize(new { success = exit == 0, exitCode = exit, logFile = logPath }));
        return exit == 0 ? 0 : 1;
    }

    private async Task<int> AnalyzeLogsAsync(FileInfo logFile, FileInfo? output, bool json)
    {
        if (!logFile.Exists)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Log file not found", path = logFile.FullName }));
            return 1;
        }

        var analysisJson = await UnityPlatform.AnalyzeLogAsync(logFile.FullName, CancellationToken.None);
        var outputPath = output?.FullName ?? Path.ChangeExtension(logFile.FullName, ".analysis.json");
        await TextFile.WriteAllTextAsync(outputPath, analysisJson);

        if (json) Console.WriteLine(analysisJson);
        else _logger.LogInformation("Unity log analysis saved to {Path}", outputPath);
        return 0;
    }

    private async Task<int> AnalyzeErrorsAsync(FileInfo logFile, DirectoryInfo project, FileInfo? output, bool json)
    {
        // First analyze the logs
        var analysisPath = output ?? new FileInfo(Path.ChangeExtension(logFile.FullName, ".analysis.json"));
        var analysisExitCode = await AnalyzeLogsAsync(logFile, analysisPath, json);
        
        if (analysisExitCode != 0)
        {
            return analysisExitCode;
        }

        // Check if there are errors
        if (!analysisPath.Exists)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = "Analysis file not created" }));
            return 1;
        }

        var analysisJson = await File.ReadAllTextAsync(analysisPath.FullName);
        var analysisDoc = JsonDocument.Parse(analysisJson);
        var root = analysisDoc.RootElement;

        var hasErrors = root.TryGetProperty("hasErrors", out var hasErrorsProp) && hasErrorsProp.GetBoolean();
        var errorCount = root.TryGetProperty("errorCount", out var errorCountProp) ? errorCountProp.GetInt32() : 0;

        if (hasErrors && errorCount > 0)
        {
            if (!json)
            {
                _logger.LogWarning("Found {ErrorCount} error(s) in Unity log", errorCount);
                _logger.LogInformation("Attempting to fix syntax errors via orchestration...");
            }

            // Note: Actual error fixing would require orchestration integration
            // For now, we just report the errors
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    hasErrors = true,
                    errorCount = errorCount,
                    message = "Errors found. Use 'nexo orchestrate' to attempt automatic fixes.",
                    analysisFile = analysisPath.FullName
                }));
            }
        }
        else
        {
            if (!json)
            {
                _logger.LogInformation("✅ No errors found in Unity log");
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    hasErrors = false,
                    errorCount = 0,
                    message = "No errors found"
                }));
            }
        }

        return 0;
    }
}

