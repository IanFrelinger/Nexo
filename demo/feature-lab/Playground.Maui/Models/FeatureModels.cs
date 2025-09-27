namespace Playground.Maui.Models;

public partial class RunResult
{
    public string RunId { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<StepEvent> Steps { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime StartedAt { get; set; }
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

public partial class FeatureTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

public partial class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Messages { get; set; } = new();
    public Dictionary<string, bool> Checks { get; set; } = new();
}
