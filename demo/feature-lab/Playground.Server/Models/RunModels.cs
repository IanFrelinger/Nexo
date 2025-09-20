namespace Playground.Server.Models;

public class RunResult
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public string FeatureName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string ErrorMessage { get; set; } = string.Empty;
    public List<StepEvent> Steps { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public record StepEvent(
    string StepId,
    string StepName,
    string Status,
    string? ErrorMessage,
    Dictionary<string, object>? Outputs,
    DateTime Timestamp
);

public record ApprovalEvent(
    string RunId,
    string StepId,
    string Reason,
    string Status,
    DateTime Timestamp
);

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ValidationCheck> Checks { get; set; } = new();
}

public class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
