namespace Nexo.Core.Application.Copilot.Models;

public sealed class CopilotTaskRecord
{
    public string TaskId { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? Summary { get; set; }
    public string? Error { get; set; }
}
