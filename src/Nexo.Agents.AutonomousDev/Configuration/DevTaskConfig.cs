using Nexo.Agents.AutonomousDev.Models;

namespace Nexo.Agents.AutonomousDev.Configuration;

/// <summary>
/// Describes what to build. Can be minimal (AI infers details) or detailed.
/// </summary>
public record DevTaskConfig
{
    /// <summary>
    /// What to build. Natural language is fine.
    /// Examples:
    /// - "Add a save/load system for player progress"
    /// - "Fix the bug where enemies don't respawn after death"
    /// - "Create a REST API endpoint for user registration"
    /// - "Improve the checkout page conversion rate"
    /// </summary>
    public required string Task { get; init; }
    
    /// <summary>
    /// Path to the project to modify.
    /// </summary>
    public required string ProjectPath { get; init; }
    
    /// <summary>
    /// Optional: What type of project is this?
    /// </summary>
    public ProjectType? ProjectType { get; init; }
    
    /// <summary>
    /// Optional: Detailed requirements, acceptance criteria, or specs.
    /// Can be a file path, URL, or inline text.
    /// </summary>
    public string? DetailedSpec { get; init; }
    
    /// <summary>
    /// Optional: Reference materials (existing code, docs, examples).
    /// </summary>
    public string[] References { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// How to verify the task is complete. AI infers if not provided.
    /// Examples:
    /// - "Player can save game, quit, reload, and continue from same point"
    /// - "API returns 201 for valid registration, 400 for invalid"
    /// </summary>
    public string? AcceptanceCriteria { get; init; }
    
    /// <summary>
    /// Maximum iterations before giving up.
    /// </summary>
    public int MaxIterations { get; init; } = 10;
    
    /// <summary>
    /// How much autonomy does the agent have?
    /// </summary>
    public AutonomyLevel Autonomy { get; init; } = AutonomyLevel.Supervised;
    
    /// <summary>
    /// Target for the Universal Tester to test against.
    /// AI infers from project type if not provided.
    /// </summary>
    public string? TestTarget { get; init; }
    
    /// <summary>
    /// What persona should the mock user have?
    /// </summary>
    public MockUserPersona TestPersona { get; init; } = MockUserPersona.Average;
    
    /// <summary>
    /// Additional constraints or guidelines.
    /// </summary>
    public string[] Constraints { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Type of project being developed.
/// </summary>
public enum ProjectType
{
    Auto,
    UnityGame,
    UnrealGame,
    DotNetApp,
    DotNetApi,
    ReactApp,
    VueApp,
    NextJs,
    PythonApp,
    NodeApi,
    Generic
}

/// <summary>
/// How much autonomy does the agent have?
/// </summary>
public enum AutonomyLevel
{
    /// <summary>
    /// Show plan before executing, ask for approval at each major step.
    /// </summary>
    Supervised,
    
    /// <summary>
    /// Execute automatically but don't commit/deploy without approval.
    /// </summary>
    SemiAutonomous,
    
    /// <summary>
    /// Full autonomy - iterate until done, commit when complete.
    /// </summary>
    FullyAutonomous
}
