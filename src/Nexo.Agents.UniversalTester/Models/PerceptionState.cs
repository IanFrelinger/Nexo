namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// Everything the agent can perceive about the current state.
/// Different adapters populate different fields.
/// </summary>
public record PerceptionState
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    // Visual
    public byte[]? Screenshot { get; init; }
    public byte[]? PreviousScreenshot { get; init; }
    public double? VisualChangePercent { get; init; }
    
    // Structural (web/desktop)
    public string? DomSnapshot { get; init; }
    public string? AccessibilityTree { get; init; }
    public IReadOnlyList<InteractiveElement> InteractiveElements { get; init; } = Array.Empty<InteractiveElement>();
    
    // Game-specific
    public GameState? GameState { get; init; }
    public IReadOnlyList<GameObject> VisibleObjects { get; init; } = Array.Empty<GameObject>();
    public PlayerState? PlayerState { get; init; }
    
    // API-specific
    public ApiResponse? LastApiResponse { get; init; }
    public IReadOnlyList<ApiEndpoint> AvailableEndpoints { get; init; } = Array.Empty<ApiEndpoint>();
    
    // CLI-specific
    public string? TerminalOutput { get; init; }
    public string? CurrentPrompt { get; init; }
    
    // Universal
    public IReadOnlyList<string> ConsoleLog { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public PerformanceMetrics? Performance { get; init; }
    public string? CurrentUrl { get; init; }
    public string? WindowTitle { get; init; }
}

public record InteractiveElement
{
    public required string Id { get; init; }
    public required string Type { get; init; }  // button, input, link, slider, etc.
    public string? Label { get; init; }
    public string? Description { get; init; }
    public BoundingBox? Bounds { get; init; }
    public bool IsVisible { get; init; }
    public bool IsEnabled { get; init; }
    public string? CurrentValue { get; init; }
    public IReadOnlyList<string> PossibleActions { get; init; } = Array.Empty<string>();
}

public record BoundingBox
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

public record GameState
{
    public string? CurrentScene { get; init; }
    public string? CurrentLevel { get; init; }
    public bool IsPaused { get; init; }
    public bool IsInMenu { get; init; }
    public bool IsInCutscene { get; init; }
    public bool IsLoading { get; init; }
    public Dictionary<string, object> CustomState { get; init; } = new();
}

public record GameObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public Vector3? Position { get; init; }
    public bool IsInteractable { get; init; }
}

public record PlayerState
{
    public Vector3? Position { get; init; }
    public float? Health { get; init; }
    public IReadOnlyList<string> Inventory { get; init; } = Array.Empty<string>();
    public Dictionary<string, object> Stats { get; init; } = new();
}

public record Vector3
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}

public record Vector2
{
    public double X { get; init; }
    public double Y { get; init; }
}

public record ApiResponse
{
    public int StatusCode { get; init; }
    public string? Body { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public TimeSpan Duration { get; init; }
}

public record ApiEndpoint
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

public record PerformanceMetrics
{
    public double? Fps { get; init; }
    public long? MemoryUsageMb { get; init; }
    public double? CpuPercent { get; init; }
    public double? GpuPercent { get; init; }
    public TimeSpan? FrameTime { get; init; }
    public int? DrawCalls { get; init; }
}
