using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Message emitted when an agent encounters an error.
/// 
/// Contains error message, type, and optional stack trace.
/// Used to notify other agents and the orchestrator about failures.
/// </summary>
public sealed record AgentError : AgentMessage
{
    public AgentError()
    {
        MessageType = "AgentError";
    }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Error type/category.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Stack trace (if available).
    /// </summary>
    public string? StackTrace { get; init; }
}
