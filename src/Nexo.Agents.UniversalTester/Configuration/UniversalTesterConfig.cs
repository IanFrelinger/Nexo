namespace Nexo.Agents.UniversalTester.Configuration;

/// <summary>
/// Minimal configuration. The AI figures out everything else.
/// </summary>
public record UniversalTesterConfig
{
    /// <summary>
    /// What to test. Can be:
    /// - URL: "https://my-app.com"
    /// - File path: "C:\Games\MyGame.exe"
    /// - API endpoint: "api://https://api.example.com"
    /// - CLI command: "cli://dotnet run"
    /// - Process name: "process://MyApp"
    /// </summary>
    public required string Target { get; init; }
    
    /// <summary>
    /// Natural language description of what to test.
    /// Examples:
    /// - "Test the checkout flow and make sure payment works"
    /// - "Play through the tutorial level and check for bugs"
    /// - "Verify all CRUD operations work on the API"
    /// - "Stress test the login form with invalid inputs"
    /// - "Explore the game and find any crashes or visual glitches"
    /// </summary>
    public required string Goal { get; init; }
    
    /// <summary>
    /// Optional: What kind of thing is this? AI will infer if not provided.
    /// </summary>
    public TargetType? TargetType { get; init; }
    
    /// <summary>
    /// Optional: Specific things to look for or avoid.
    /// </summary>
    public string[] Constraints { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// How thorough should testing be? Affects time and cost.
    /// </summary>
    public TestingDepth Depth { get; init; } = TestingDepth.Standard;
    
    /// <summary>
    /// Maximum time to spend testing.
    /// </summary>
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// Optional: Authentication/setup instructions in natural language.
    /// Example: "Login with username 'test@example.com' and password 'test123'"
    /// </summary>
    public string? SetupInstructions { get; init; }
    
    /// <summary>
    /// Optional: What success looks like in natural language.
    /// Example: "User should be able to complete purchase and see confirmation"
    /// </summary>
    public string? SuccessCriteria { get; init; }
}

/// <summary>
/// Type of target application being tested.
/// </summary>
public enum TargetType
{
    Auto,       // AI determines
    WebApp,
    Game,
    DesktopApp,
    MobileApp,
    Api,
    Cli
}

/// <summary>
/// How thorough the testing should be.
/// </summary>
public enum TestingDepth
{
    Quick,      // Fast smoke test (~2 min)
    Standard,   // Balanced coverage (~10 min)
    Thorough,   // Deep exploration (~30 min)
    Exhaustive  // Find everything (~60+ min)
}
