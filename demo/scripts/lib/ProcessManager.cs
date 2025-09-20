using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Manages long-running processes with proper lifecycle management
/// </summary>
public class ProcessManager : IDisposable
{
    private readonly List<Process> _managedProcesses = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;

    /// <summary>
    /// Starts a managed process
    /// </summary>
    public async Task<ManagedProcess> StartProcessAsync(
        string fileName,
        string arguments = "",
        string workingDirectory = null,
        Dictionary<string, string> environmentVariables = null,
        bool redirectOutput = true)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput,
                CreateNoWindow = true
            }
        };

        // Set environment variables
        if (environmentVariables != null)
        {
            foreach (var env in environmentVariables)
            {
                process.StartInfo.EnvironmentVariables[env.Key] = env.Value;
            }
        }

        var managedProcess = new ManagedProcess(process, _cancellationTokenSource.Token);
        _managedProcesses.Add(process);

        try
        {
            process.Start();
            
            if (redirectOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return managedProcess;
        }
        catch (Exception ex)
        {
            _managedProcesses.Remove(process);
            throw new InvalidOperationException($"Failed to start process {fileName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Starts a .NET application
    /// </summary>
    public async Task<ManagedProcess> StartDotNetAppAsync(
        string projectPath,
        string arguments = "",
        string framework = null,
        string configuration = "Release",
        Dictionary<string, string> environmentVariables = null)
    {
        var dotnetArgs = new List<string> { "run" };
        
        if (!string.IsNullOrEmpty(framework))
        {
            dotnetArgs.AddRange(new[] { "-f", framework });
        }
        
        if (!string.IsNullOrEmpty(configuration))
        {
            dotnetArgs.AddRange(new[] { "-c", configuration });
        }
        
        if (!string.IsNullOrEmpty(arguments))
        {
            dotnetArgs.Add("--");
            dotnetArgs.Add(arguments);
        }

        return await StartProcessAsync(
            "dotnet",
            string.Join(" ", dotnetArgs),
            Path.GetDirectoryName(projectPath),
            environmentVariables);
    }

    /// <summary>
    /// Starts a web application and waits for it to be ready
    /// </summary>
    public async Task<ManagedProcess> StartWebAppAsync(
        string projectPath,
        int port,
        string framework = null,
        string configuration = "Release",
        TimeSpan? startupTimeout = null)
    {
        startupTimeout ??= TimeSpan.FromMinutes(2);

        // Set environment variables for the web app
        var envVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = $"http://localhost:{port}",
            ["ASPNETCORE_ENVIRONMENT"] = "Development"
        };

        var process = await StartDotNetAppAsync(projectPath, "", framework, configuration, envVars);

        // Wait for the application to be ready
        var isReady = await SystemUtils.WaitForConditionAsync(
            () => SystemUtils.IsPortAvailable(port) == false, // Port should be in use when ready
            startupTimeout.Value,
            TimeSpan.FromSeconds(1));

        if (!isReady)
        {
            await process.StopAsync();
            throw new TimeoutException($"Web application failed to start within {startupTimeout.Value.TotalSeconds} seconds");
        }

        return process;
    }

    /// <summary>
    /// Stops all managed processes
    /// </summary>
    public async Task StopAllProcessesAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);

        var stopTasks = new List<Task>();
        
        foreach (var process in _managedProcesses.ToArray())
        {
            if (!process.HasExited)
            {
                stopTasks.Add(StopProcessAsync(process, timeout.Value));
            }
        }

        await Task.WhenAll(stopTasks);
    }

    /// <summary>
    /// Stops a specific process
    /// </summary>
    private async Task StopProcessAsync(Process process, TimeSpan timeout)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            // Try graceful shutdown first
            process.CloseMainWindow();

            // Wait for graceful shutdown
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                // Force kill if graceful shutdown failed
                process.Kill();
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - we're cleaning up
            Console.WriteLine($"Warning: Error stopping process {process.ProcessName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all managed processes
    /// </summary>
    public IReadOnlyList<Process> GetManagedProcesses()
    {
        return _managedProcesses.AsReadOnly();
    }

    /// <summary>
    /// Checks if any managed processes are still running
    /// </summary>
    public bool HasRunningProcesses()
    {
        return _managedProcesses.Exists(p => !p.HasExited);
    }

    /// <summary>
    /// Disposes of all managed processes
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();

        // Stop all processes
        StopAllProcessesAsync().Wait();

        // Dispose of processes
        foreach (var process in _managedProcesses)
        {
            try
            {
                process.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _managedProcesses.Clear();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Represents a managed process with lifecycle tracking
/// </summary>
public class ManagedProcess : IDisposable
{
    private readonly Process _process;
    private readonly CancellationToken _cancellationToken;
    private bool _disposed = false;

    public ManagedProcess(Process process, CancellationToken cancellationToken)
    {
        _process = process;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the underlying process
    /// </summary>
    public Process Process => _process;

    /// <summary>
    /// Gets whether the process has exited
    /// </summary>
    public bool HasExited => _process.HasExited;

    /// <summary>
    /// Gets the process ID
    /// </summary>
    public int Id => _process.Id;

    /// <summary>
    /// Gets the process name
    /// </summary>
    public string ProcessName => _process.ProcessName;

    /// <summary>
    /// Waits for the process to exit
    /// </summary>
    public async Task WaitForExitAsync()
    {
        await _process.WaitForExitAsync(_cancellationToken);
    }

    /// <summary>
    /// Waits for the process to exit with a timeout
    /// </summary>
    public async Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await _process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Stops the process
    /// </summary>
    public async Task StopAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);

        if (_process.HasExited)
        {
            return;
        }

        try
        {
            // Try graceful shutdown first
            _process.CloseMainWindow();

            // Wait for graceful shutdown
            if (!await WaitForExitAsync(timeout.Value))
            {
                // Force kill if graceful shutdown failed
                _process.Kill();
                await WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to stop process {_process.ProcessName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Kills the process immediately
    /// </summary>
    public void Kill()
    {
        if (!_process.HasExited)
        {
            _process.Kill();
        }
    }

    /// <summary>
    /// Gets the standard output as a stream
    /// </summary>
    public StreamReader StandardOutput => _process.StandardOutput;

    /// <summary>
    /// Gets the standard error as a stream
    /// </summary>
    public StreamReader StandardError => _process.StandardError;

    /// <summary>
    /// Disposes of the managed process
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
        catch
        {
            // Ignore errors during disposal
        }
        finally
        {
            _process.Dispose();
            _disposed = true;
        }
    }
}
