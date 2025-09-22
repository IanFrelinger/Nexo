using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Abstractions;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Base interface for all tools.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the tool manifest describing this tool's capabilities.
    /// </summary>
    ToolManifest Manifest { get; }

    /// <summary>
    /// Executes the tool with the given input.
    /// </summary>
    /// <param name="input">JSON input as string</param>
    /// <param name="context">Tool execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON output as string</returns>
    Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed tool interface.
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
public interface ITool<TInput, TOutput> : ITool
{
    /// <summary>
    /// Executes the tool with strongly-typed input.
    /// </summary>
    /// <param name="input">Typed input</param>
    /// <param name="context">Tool execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed output</returns>
    Task<TOutput> ExecuteAsync(TInput input, ToolContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tool execution context providing access to logging, cancellation, and permissions.
/// </summary>
/// <param name="Logger">Logger for the tool</param>
/// <param name="CancellationToken">Cancellation token</param>
/// <param name="Timeout">Tool execution timeout</param>
/// <param name="Permissions">Available permissions</param>
/// <param name="FileSystem">Sandboxed file system access</param>
/// <param name="WorkingDirectory">Current working directory</param>
public record ToolContext(
    Microsoft.Extensions.Logging.ILogger Logger,
    CancellationToken CancellationToken,
    TimeSpan Timeout,
    ToolPermissions Permissions,
    IFileSystem FileSystem,
    string WorkingDirectory);

