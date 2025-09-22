using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Broker for executing tools with proper permissions and observability.
/// </summary>
public interface IToolBroker
{
    /// <summary>
    /// Executes a tool by its ID with the given input.
    /// </summary>
    /// <param name="toolId">Tool identifier</param>
    /// <param name="input">JSON input</param>
    /// <param name="context">Tool execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<ToolExecutionResult> ExecuteToolAsync(
        string toolId,
        string input,
        ToolContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a tool with strongly-typed input and output.
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="toolId">Tool identifier</param>
    /// <param name="input">Typed input</param>
    /// <param name="context">Tool execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result with typed output</returns>
    Task<ToolExecutionResult<TOutput>> ExecuteToolAsync<TInput, TOutput>(
        string toolId,
        TInput input,
        ToolContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a tool can be executed with the given permissions.
    /// </summary>
    /// <param name="toolId">Tool identifier</param>
    /// <param name="permissions">Available permissions</param>
    /// <returns>True if the tool can be executed</returns>
    bool CanExecuteTool(string toolId, ToolPermissions permissions);
}

/// <summary>
/// Result of a tool execution.
/// </summary>
/// <param name="Success">Whether the tool executed successfully</param>
/// <param name="Output">Tool output (JSON string)</param>
/// <param name="Error">Error message if execution failed</param>
/// <param name="Duration">Execution duration</param>
/// <param name="ToolId">Tool identifier</param>
public record ToolExecutionResult(
    bool Success,
    string? Output,
    string? Error,
    TimeSpan Duration,
    string ToolId);

/// <summary>
/// Result of a tool execution with typed output.
/// </summary>
/// <typeparam name="TOutput">Output type</typeparam>
/// <param name="Success">Whether the tool executed successfully</param>
/// <param name="Output">Typed output</param>
/// <param name="Error">Error message if execution failed</param>
/// <param name="Duration">Execution duration</param>
/// <param name="ToolId">Tool identifier</param>
public record ToolExecutionResult<TOutput>(
    bool Success,
    TOutput? Output,
    string? Error,
    TimeSpan Duration,
    string ToolId);
