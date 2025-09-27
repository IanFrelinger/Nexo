namespace Playground.Console.Models;

public partial class FeatureInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

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

public partial class StepEvent
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Outputs { get; set; }
    public DateTime Timestamp { get; set; }
}

public partial class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationCheck> Checks { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public partial class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
}
