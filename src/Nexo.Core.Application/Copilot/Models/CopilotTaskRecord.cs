namespace Nexo.Core.Application.Copilot.Models;

/// <summary>Persisted record of a Copilot task submission and outcome.</summary>
public sealed class CopilotTaskRecord
{
    /// <summary>Logical tenant / org partition for isolation (defaults to <c>default</c>).</summary>
    public string TenantId { get; set; } = "default";

    /// <summary>Unique task identifier.</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>Task description or prompt text.</summary>
    public string Task { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the task was submitted.</summary>
    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>UTC timestamp when the task completed, if finished.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Whether the task completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable outcome summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; set; }
}
