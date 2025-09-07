using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces;
using Nexo.Shared.Models;

namespace Nexo.Infrastructure.Adapters.Command
{
/// <summary>
/// Provides functionality to execute system commands, manage their execution,
/// and handle the results using the Process API.
/// </summary>
public sealed class ProcessCommandExecutor : ICommandExecutor
{
    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> used to log diagnostic information
    /// and execution details within the <see cref="ProcessCommandExecutor"/> class.
    /// </summary>
    /// <remarks>
    /// This logger is used to log debug information, such as when commands are executed,
    /// along with their arguments, execution duration, and exit codes. It also captures warnings
    /// and errors, such as command timeouts or execution failures.
    /// </remarks>
    private readonly ILogger<ProcessCommandExecutor> _logger;

    /// <summary>
    /// A semaphore used to limit the number of concurrent executions of command processing operations.
    /// </summary>
    /// <remarks>
    /// The semaphore is set to allow a maximum of 10 concurrent executions, ensuring that system resource usage
    /// is effectively controlled while processing commands.
    /// </remarks>
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10); // Allow up to 10 concurrent executions

    /// <summary>
    /// Provides functionality to execute system commands using the Process API.
    /// </summary>
    public ProcessCommandExecutor(ILogger<ProcessCommandExecutor> logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }
        _logger = logger;
    }

    /// <summary>
    /// Executes a system command asynchronously using the specified command and arguments.
    /// </summary>
    /// <param name="command">The system command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="cancellationToken">Optional. A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous execution. The task result contains a <see cref="CommandResult"/> with details about the execution outcome.</returns>
    public Task<Nexo.Shared.Models.CommandResult> ExecuteAsync(
        string command, 
        string arguments,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            new CommandRequest { Command = command, Arguments = arguments }, 
            cancellationToken);
    }

    /// <summary>
    /// Executes a command asynchronously with the specified command, arguments, and cancellation token.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
    public async Task<Nexo.Shared.Models.CommandResult> ExecuteAsync(
        CommandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ExecuteInternalAsync(request, null!, null!, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Executes a command request with optional output and error callbacks.
    /// </summary>
    /// <param name="request">The command request containing details about the command to execute.</param>
    /// <param name="outputCallback">An optional callback to handle standard output lines from the command execution.</param>
    /// <param name="errorCallback">An optional callback to handle error output lines from the command execution.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which resolves to a <see cref="CommandResult"/> containing the outcome of the executed command.</returns>
    public async Task<Nexo.Shared.Models.CommandResult> ExecuteWithCallbacksAsync(
        CommandRequest request,
        Action<string> outputCallback,
        Action<string> errorCallback,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ExecuteInternalAsync(request, outputCallback, errorCallback, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Determines whether a specified command is available in the current system.
    /// </summary>
    /// <param name="command">The name of the command to check for availability.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean
    /// value indicating whether the specified command is available.</returns>
    public async Task<bool> IsCommandAvailableAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));
        }
        
        try
        {
            var checkCommand = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";
            var result = await ExecuteAsync(checkCommand, command, cancellationToken);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes the given command internally, managing its lifecycle, output capture, and timeouts.
    /// </summary>
    /// <param name="request">The command request containing details such as command name, arguments, and timeout settings.</param>
    /// <param name="outputCallback">A callback delegate to handle standard output streams, if provided.</param>
    /// <param name="errorCallback">A callback delegate to handle error output streams, if provided.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task representing the asynchronous command execution operation.
    /// Returns a <see cref="CommandResult"/> containing result details, such as success status, captured output, captured error, exit code, and execution duration.
    /// </returns>
    private async Task<Nexo.Shared.Models.CommandResult> ExecuteInternalAsync(
        CommandRequest request,
        Action<string> outputCallback,
        Action<string> errorCallback,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var processInfo = CreateProcessStartInfo(request);
        
        Nexo.Shared.Models.CommandResult result;
        using (var process = new Process { StartInfo = processInfo })
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            cts.CancelAfter(request.TimeoutSeconds * 1000);
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var outputTcs = new TaskCompletionSource<bool>();
            var errorTcs = new TaskCompletionSource<bool>();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (request.CaptureOutput)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                    outputCallback?.Invoke(e.Data);
                }
                else
                {
                    outputTcs.TrySetResult(true);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (request.CaptureOutput)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                    errorCallback?.Invoke(e.Data);
                }
                else
                {
                    errorTcs.TrySetResult(true);
                }
            };
            
            try
            {
                _logger.LogDebug("Executing command: {Command} {Arguments}", request.Command, request.Arguments);
                
                if (!process.Start())
                {
                    result = new Nexo.Shared.Models.CommandResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        Error = "Failed to start process"
                    };
                }
                else
                {
                    if (request.CaptureOutput)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                    
                    process.WaitForExit();
                    
                    if (request.CaptureOutput)
                    {
                        await Task.WhenAll(outputTcs.Task, errorTcs.Task);
                    }
                    
                    var executionTime = DateTimeOffset.UtcNow - startTime;
                    var output = outputBuilder.ToString().TrimEnd();
                    var error = errorBuilder.ToString().TrimEnd();
                    
                    _logger.LogDebug(
                        "Command completed: {Command} {Arguments} - Exit Code: {ExitCode}, Duration: {Duration}ms",
                        request.Command, request.Arguments, process.ExitCode, executionTime.TotalMilliseconds);
                    
                    result = new Nexo.Shared.Models.CommandResult
                    {
                        IsSuccess = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode,
                        ExecutionTimeMs = (long)executionTime.TotalMilliseconds
                    };
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Best effort
                }
                
                var executionTime = DateTimeOffset.UtcNow - startTime;
                _logger.LogWarning(
                    "Command timed out: {Command} {Arguments} after {Timeout}ms",
                    request.Command, request.Arguments, request.TimeoutSeconds * 1000);
                
                result = new Nexo.Shared.Models.CommandResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    Error = $"Command timed out after {request.TimeoutSeconds * 1000}ms",
                    ExecutionTimeMs = (long)executionTime.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                var executionTime = DateTimeOffset.UtcNow - startTime;
                _logger.LogError(ex, "Error executing command: {Command} {Arguments}", request.Command, request.Arguments);
                result = new Nexo.Shared.Models.CommandResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    Error = ex.Message,
                    ExecutionTimeMs = (long)executionTime.TotalMilliseconds
                };
            }
        }
        return result;
    }

    /// <summary>
    /// Creates and configures a new instance of the <see cref="ProcessStartInfo"/> class
    /// based on the parameters specified in the provided <see cref="CommandRequest"/> object.
    /// </summary>
    /// <param name="request">
    /// An instance of <see cref="CommandRequest"/> containing details such as
    /// command to execute, arguments, working directory, environment variables,
    /// and execution flags.
    /// </param>
    /// <returns>
    /// A configured <see cref="ProcessStartInfo"/> object ready to be used to start a process.
    /// </returns>
    private static System.Diagnostics.ProcessStartInfo CreateProcessStartInfo(CommandRequest request)
    {
        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = request.Command,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = request.CaptureOutput,
            RedirectStandardError = request.CaptureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        // Note: EnvironmentVariables and UseShell properties are not available in the current CommandRequest model
        // These features would need to be added to the CommandRequest class if needed
        
        return processInfo;
    }
}
}