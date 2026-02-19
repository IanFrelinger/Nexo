namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// Everything the agent can perceive about the current state.
/// Different adapters populate different fields.
/// </summary>
public record PerceptionState
{
    /// <summary>When this perception was captured (UTC).</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Current screenshot from the target.</summary>
    public byte[]? Screenshot { get; init; }
    /// <summary>Previous screenshot for change detection.</summary>
    public byte[]? PreviousScreenshot { get; init; }
    /// <summary>Percent of pixels that changed between screenshots.</summary>
    public double? VisualChangePercent { get; init; }

    /// <summary>DOM/HTML snapshot (web targets).</summary>
    public string? DomSnapshot { get; init; }
    /// <summary>Accessibility tree (web/desktop targets).</summary>
    public string? AccessibilityTree { get; init; }
    /// <summary>Interactive UI elements (buttons, inputs, etc.).</summary>
    public IReadOnlyList<InteractiveElement> InteractiveElements { get; init; } = Array.Empty<InteractiveElement>();

    /// <summary>Game state (scene, level, paused; game targets).</summary>
    public GameState? GameState { get; init; }
    /// <summary>Visible game objects.</summary>
    public IReadOnlyList<GameObject> VisibleObjects { get; init; } = Array.Empty<GameObject>();
    /// <summary>Player state (health, inventory; game targets).</summary>
    public PlayerState? PlayerState { get; init; }

    /// <summary>Last API response (API targets).</summary>
    public ApiResponse? LastApiResponse { get; init; }
    /// <summary>Discovered API endpoints.</summary>
    public IReadOnlyList<ApiEndpoint> AvailableEndpoints { get; init; } = Array.Empty<ApiEndpoint>();

    /// <summary>Terminal output (CLI targets).</summary>
    public string? TerminalOutput { get; init; }
    /// <summary>Current CLI prompt.</summary>
    public string? CurrentPrompt { get; init; }

    /// <summary>Console log entries.</summary>
    public IReadOnlyList<string> ConsoleLog { get; init; } = Array.Empty<string>();
    /// <summary>Error messages from the target.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    /// <summary>Warning messages from the target.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    /// <summary>Performance metrics (FPS, memory, CPU).</summary>
    public PerformanceMetrics? Performance { get; init; }
    /// <summary>Current URL (web targets).</summary>
    public string? CurrentUrl { get; init; }
    /// <summary>Window or tab title.</summary>
    public string? WindowTitle { get; init; }
}

/// <summary>
/// A UI element that can be interacted with (button, input, link, etc.).
/// </summary>
public record InteractiveElement
{
    /// <summary>Unique element identifier (selector, automation id).</summary>
    public required string Id { get; init; }
    /// <summary>Element type: button, input, link, slider, etc.</summary>
    public required string Type { get; init; }
    /// <summary>Display label or accessible name.</summary>
    public string? Label { get; init; }
    /// <summary>Description or help text.</summary>
    public string? Description { get; init; }
    /// <summary>Screen bounds (x, y, width, height).</summary>
    public BoundingBox? Bounds { get; init; }
    /// <summary>True if the element is visible.</summary>
    public bool IsVisible { get; init; }
    /// <summary>True if the element is enabled for interaction.</summary>
    public bool IsEnabled { get; init; }
    /// <summary>Current value (e.g. input text).</summary>
    public string? CurrentValue { get; init; }
    /// <summary>Possible actions: click, type, select, etc.</summary>
    public IReadOnlyList<string> PossibleActions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Bounding rectangle for a UI element.
/// </summary>
public record BoundingBox
{
    /// <summary>Left edge X coordinate.</summary>
    public double X { get; init; }
    /// <summary>Top edge Y coordinate.</summary>
    public double Y { get; init; }
    /// <summary>Width in pixels.</summary>
    public double Width { get; init; }
    /// <summary>Height in pixels.</summary>
    public double Height { get; init; }
}

/// <summary>
/// State of a video game target (scene, level, pause, etc.).
/// </summary>
public record GameState
{
    /// <summary>Current scene or area name.</summary>
    public string? CurrentScene { get; init; }
    /// <summary>Current level or stage.</summary>
    public string? CurrentLevel { get; init; }
    /// <summary>True if the game is paused.</summary>
    public bool IsPaused { get; init; }
    /// <summary>True if in a menu (not gameplay).</summary>
    public bool IsInMenu { get; init; }
    /// <summary>True if in a cutscene.</summary>
    public bool IsInCutscene { get; init; }
    /// <summary>True if loading.</summary>
    public bool IsLoading { get; init; }
    /// <summary>Custom key-value state from the game.</summary>
    public Dictionary<string, object> CustomState { get; init; } = new();
}

/// <summary>
/// A visible object in a game (NPC, item, obstacle, etc.).
/// </summary>
public record GameObject
{
    /// <summary>Unique object identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Display name.</summary>
    public required string Name { get; init; }
    /// <summary>3D position in world space.</summary>
    public Vector3? Position { get; init; }
    /// <summary>True if the player can interact with this object.</summary>
    public bool IsInteractable { get; init; }
}

/// <summary>
/// State of the player in a game target.
/// </summary>
public record PlayerState
{
    /// <summary>Player position in world space.</summary>
    public Vector3? Position { get; init; }
    /// <summary>Current health (0-100 or similar).</summary>
    public float? Health { get; init; }
    /// <summary>Items in inventory.</summary>
    public IReadOnlyList<string> Inventory { get; init; } = Array.Empty<string>();
    /// <summary>Custom stats (ammo, score, etc.).</summary>
    public Dictionary<string, object> Stats { get; init; } = new();
}

/// <summary>
/// 3D vector (position, direction).
/// </summary>
public record Vector3
{
    /// <summary>X component.</summary>
    public float X { get; init; }
    /// <summary>Y component.</summary>
    public float Y { get; init; }
    /// <summary>Z component.</summary>
    public float Z { get; init; }
}

/// <summary>
/// 2D vector (screen coordinates, etc.).
/// </summary>
public record Vector2
{
    /// <summary>X component.</summary>
    public double X { get; init; }
    /// <summary>Y component.</summary>
    public double Y { get; init; }
}

/// <summary>
/// Response from an API request.
/// </summary>
public record ApiResponse
{
    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; init; }
    /// <summary>Response body (JSON, text).</summary>
    public string? Body { get; init; }
    /// <summary>Response headers.</summary>
    public Dictionary<string, string> Headers { get; init; } = new();
    /// <summary>Request duration.</summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// An API endpoint (method + path).
/// </summary>
public record ApiEndpoint
{
    /// <summary>HTTP method (GET, POST, etc.).</summary>
    public required string Method { get; init; }
    /// <summary>URL path.</summary>
    public required string Path { get; init; }
    /// <summary>Endpoint description from OpenAPI.</summary>
    public string? Description { get; init; }
    /// <summary>Parameter schema if available.</summary>
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Performance metrics from the target (FPS, memory, CPU).
/// </summary>
public record PerformanceMetrics
{
    /// <summary>Frames per second.</summary>
    public double? Fps { get; init; }
    /// <summary>Memory usage in megabytes.</summary>
    public long? MemoryUsageMb { get; init; }
    /// <summary>CPU usage percent.</summary>
    public double? CpuPercent { get; init; }
    /// <summary>GPU usage percent.</summary>
    public double? GpuPercent { get; init; }
    /// <summary>Frame time (ms per frame).</summary>
    public TimeSpan? FrameTime { get; init; }
    /// <summary>Draw call count.</summary>
    public int? DrawCalls { get; init; }
}
