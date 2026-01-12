namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// AI's interpretation of what it's seeing and what's possible.
/// </summary>
public record Understanding
{
    /// <summary>
    /// What type of application/screen is this?
    /// Examples: "Login form", "Game main menu", "Product listing", "Combat gameplay"
    /// </summary>
    public required string ScreenType { get; init; }
    
    /// <summary>
    /// What is the current context/state?
    /// Examples: "User is not logged in", "Player is in combat", "Cart has 3 items"
    /// </summary>
    public required string CurrentContext { get; init; }
    
    /// <summary>
    /// What actions are available right now?
    /// </summary>
    public IReadOnlyList<AvailableAction> AvailableActions { get; init; } = Array.Empty<AvailableAction>();
    
    /// <summary>
    /// What is the agent trying to accomplish? (interpreted from goal)
    /// </summary>
    public required string CurrentObjective { get; init; }
    
    /// <summary>
    /// How much progress has been made toward the goal? (0-100)
    /// </summary>
    public int ProgressPercent { get; init; }
    
    /// <summary>
    /// Any issues or anomalies detected?
    /// </summary>
    public IReadOnlyList<DetectedIssue> Issues { get; init; } = Array.Empty<DetectedIssue>();
    
    /// <summary>
    /// What areas haven't been explored yet?
    /// </summary>
    public IReadOnlyList<string> UnexploredAreas { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Confidence in this understanding (0-1)
    /// </summary>
    public double Confidence { get; init; }
}

public record AvailableAction
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required string Type { get; init; }  // click, type, navigate, api_call, key_press, etc.
    public string? Target { get; init; }
    public string? Value { get; init; }
    public double RelevanceToGoal { get; init; }  // How likely this helps achieve the goal (0-1)
    public double ExplorationValue { get; init; }  // How much new ground this covers (0-1)
    public double RiskLevel { get; init; }  // Chance of breaking something (0-1)
}

public record DetectedIssue
{
    public required string Type { get; init; }  // visual_glitch, error, performance, accessibility, ux, crash
    public required string Description { get; init; }
    public required IssueSeverity Severity { get; init; }
    public byte[]? Screenshot { get; init; }
    public string? Evidence { get; init; }
    public BoundingBox? Location { get; init; }
}

public enum IssueSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}
