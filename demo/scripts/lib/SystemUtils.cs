using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// System utilities for robust command execution and system operations
/// </summary>
public static class SystemUtils
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly int DefaultMaxRetries = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Executes a command with timeout protection
    /// </summary>
    public static async Task<CommandResult> RunWithTimeoutAsync(
        string command, 
        string arguments = "", 
        TimeSpan? timeout = null,
        string workingDirectory = null,
        bool captureOutput = true)
    {
        timeout ??= DefaultTimeout;
        
        using var cts = new CancellationTokenSource(timeout.Value);
        using var process = new Process();
        
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            CreateNoWindow = true
        };

        var output = new StringWriter();
        var error = new StringWriter();

        if (captureOutput)
        {
            process.OutputDataReceived += (_, e) => output.WriteLine(e.Data);
            process.ErrorDataReceived += (_, e) => error.WriteLine(e.Data);
        }

        try
        {
            process.Start();
            
            if (captureOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            await process.WaitForExitAsync(cts.Token);
            
            return new CommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output.ToString(),
                StandardError = error.ToString(),
                Success = process.ExitCode == 0,
                TimedOut = false
            };
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }

            return new CommandResult
            {
                ExitCode = -1,
                StandardOutput = output.ToString(),
                StandardError = error.ToString(),
                Success = false,
                TimedOut = true
            };
        }
    }

    /// <summary>
    /// Executes a command with retries
    /// </summary>
    public static async Task<CommandResult> SafeExecuteAsync(
        string command,
        string arguments = "",
        int maxRetries = -1,
        TimeSpan? retryDelay = null,
        TimeSpan? timeout = null,
        string workingDirectory = null)
    {
        maxRetries = maxRetries == -1 ? DefaultMaxRetries : maxRetries;
        retryDelay ??= DefaultRetryDelay;
        
        CommandResult lastResult = null;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            lastResult = await RunWithTimeoutAsync(command, arguments, timeout, workingDirectory);
            
            if (lastResult.Success)
            {
                return lastResult;
            }
            
            if (attempt < maxRetries)
            {
                await Task.Delay(retryDelay.Value);
            }
        }
        
        return lastResult;
    }

    /// <summary>
    /// Checks if a command exists in the system PATH
    /// </summary>
    public static bool CommandExists(string command)
    {
        try
        {
            // Use a more reliable method that doesn't require external commands
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            return process.ExitCode == 0 || process.ExitCode == 1; // Some commands return 1 for --version but still exist
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the version of a command
    /// </summary>
    public static async Task<string> GetCommandVersionAsync(string command, string versionFlag = "--version")
    {
        try
        {
            var result = await RunWithTimeoutAsync(command, versionFlag, TimeSpan.FromSeconds(10));
            return result.Success ? result.StandardOutput.Trim() : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Checks if running with elevated privileges
    /// </summary>
    public static bool IsElevated()
    {
        try
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            else
            {
                // On Unix-like systems, check if EUID is 0
                return Environment.GetEnvironmentVariable("SUDO_USER") != null || 
                       Environment.GetEnvironmentVariable("USER") == "root";
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if sudo is available and working
    /// </summary>
    public static async Task<bool> CheckSudoAsync()
    {
        if (!CommandExists("sudo"))
        {
            return false;
        }

        try
        {
            var result = await RunWithTimeoutAsync("sudo", "-n true", TimeSpan.FromSeconds(5));
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely executes a command with proper error handling
    /// </summary>
    public static async Task<CommandResult> SafeExecuteAsync(string command, string arguments, string workingDirectory = null)
    {
        try
        {
            return await RunWithTimeoutAsync(command, arguments, TimeSpan.FromMinutes(5), workingDirectory);
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                ExitCode = -1,
                StandardOutput = "",
                StandardError = ex.Message
            };
        }
    }

    /// <summary>
    /// Checks if a port is available
    /// </summary>
    public static bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Finds an available port in a range
    /// </summary>
    public static int FindAvailablePort(int startPort = 8000, int maxPort = 9000)
    {
        for (int port = startPort; port <= maxPort; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }
        throw new InvalidOperationException($"No available port found in range {startPort}-{maxPort}");
    }

    /// <summary>
    /// Kills processes running on a specific port
    /// </summary>
    public static async Task<bool> KillProcessOnPortAsync(int port)
    {
        try
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows: use netstat and taskkill
                var result = await RunWithTimeoutAsync("netstat", $"-ano | findstr :{port}", TimeSpan.FromSeconds(10));
                if (result.Success && !string.IsNullOrEmpty(result.StandardOutput))
                {
                    var lines = result.StandardOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                        {
                            await RunWithTimeoutAsync("taskkill", $"/F /PID {pid}", TimeSpan.FromSeconds(5));
                        }
                    }
                }
            }
            else
            {
                // Unix-like: use lsof and kill
                var result = await RunWithTimeoutAsync("lsof", $"-ti :{port}", TimeSpan.FromSeconds(10));
                if (result.Success && !string.IsNullOrEmpty(result.StandardOutput))
                {
                    var pids = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pid in pids)
                    {
                        await RunWithTimeoutAsync("kill", $"-9 {pid}", TimeSpan.FromSeconds(5));
                    }
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a directory safely
    /// </summary>
    public static bool SafeCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for a condition with timeout
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition, 
        TimeSpan timeout, 
        TimeSpan checkInterval)
    {
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (condition())
            {
                return true;
            }
            
            await Task.Delay(checkInterval);
        }
        
        return false;
    }
}

/// <summary>
/// Result of a command execution
/// </summary>
public class CommandResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool TimedOut { get; set; }
}
