using System.Text.Json.Serialization;
using Nexo.Agent.Contracts;

namespace Nexo.Agent.Models;

/// <summary>
/// Request to create a new tool.
/// </summary>
/// <param name="Id">Desired tool identifier</param>
/// <param name="Name">Human-readable tool name</param>
/// <param name="Description">Tool description</param>
/// <param name="InputSchema">JSON schema for input validation</param>
/// <param name="OutputSchema">JSON schema for output validation</param>
/// <param name="RequiredPermissions">Required permissions</param>
/// <param name="Capabilities">Desired capabilities</param>
/// <param name="QualityTargets">Quality targets for generation</param>
/// <param name="BreakPolicy">Whether to intentionally break policy for testing</param>
/// <param name="Timeout">Tool execution timeout</param>
/// <param name="CreatedAt">Request timestamp</param>
public record ToolRequest(
    string Id,
    string Name,
    string Description,
    string InputSchema,
    string OutputSchema,
    ToolPermissions RequiredPermissions,
    IReadOnlyList<string> Capabilities,
    ToolQualityTargets QualityTargets,
    bool BreakPolicy = false,
    TimeSpan? Timeout = null,
    DateTime? CreatedAt = null)
{
    /// <summary>
    /// Creates a tool request with default values.
    /// </summary>
    public static ToolRequest Create(
        string id,
        string name,
        string description,
        ToolPermissions requiredPermissions = ToolPermissions.None,
        IReadOnlyList<string>? capabilities = null,
        ToolQualityTargets? qualityTargets = null,
        bool breakPolicy = false)
    {
        return new ToolRequest(
            Id: id,
            Name: name,
            Description: description,
            InputSchema: "{}",
            OutputSchema: "{}",
            RequiredPermissions: requiredPermissions,
            Capabilities: capabilities ?? Array.Empty<string>(),
            QualityTargets: qualityTargets ?? ToolQualityTargets.Default,
            BreakPolicy: breakPolicy,
            Timeout: TimeSpan.FromMinutes(5),
            CreatedAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Quality targets for tool generation.
/// </summary>
/// <param name="MinPolicyScore">Minimum policy compliance score (0-100)</param>
/// <param name="MaxRepairAttempts">Maximum repair attempts</param>
/// <param name="RequireTests">Whether to require unit tests</param>
/// <param name="RequireDocumentation">Whether to require documentation</param>
public record ToolQualityTargets(
    int MinPolicyScore = 80,
    int MaxRepairAttempts = 3,
    bool RequireTests = true,
    bool RequireDocumentation = true)
{
    /// <summary>
    /// Default quality targets.
    /// </summary>
    public static ToolQualityTargets Default => new();
}

/// <summary>
/// Natural language project requirements for decomposition.
/// </summary>
public record ProjectRequirements
{
    public string Description { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Timeline { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Context for command decomposition.
/// </summary>
public record CommandDecompositionContext
{
    public string TargetRuntime { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = string.Empty;
    public string[] AvailableLibraries { get; init; } = Array.Empty<string>();
    public string[] DocumentationSources { get; init; } = Array.Empty<string>();
    public Dictionary<string, object> RuntimeParameters { get; init; } = new();
}

/// <summary>
/// Result of command decomposition.
/// </summary>
public record CommandDecompositionResult
{
    public string OriginalPrompt { get; init; } = string.Empty;
    public CommandSequence Commands { get; init; } = new();
    public CommandReuseAnalysis ReuseAnalysis { get; init; } = new();
    public DocumentationCrossReference[] DocumentationReferences { get; init; } = Array.Empty<DocumentationCrossReference>();
    public string[] GeneratedCodeFiles { get; init; } = Array.Empty<string>();
    public TimeSpan EstimatedExecutionTime { get; init; }
    public string[] Dependencies { get; init; } = Array.Empty<string>();
}

/// <summary>
/// A sequence of commands to be executed.
/// </summary>
public record CommandSequence
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Command[] Commands { get; init; } = Array.Empty<Command>();
    public CommandDependency[] Dependencies { get; init; } = Array.Empty<CommandDependency>();
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// A single reusable command.
/// </summary>
public record Command
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string[] Tags { get; init; } = Array.Empty<string>();
    public int ReuseCount { get; init; }
    public DateTime LastUsed { get; init; }
}

/// <summary>
/// Dependency between commands.
/// </summary>
public record CommandDependency
{
    public string FromCommandId { get; init; } = string.Empty;
    public string ToCommandId { get; init; } = string.Empty;
    public string OutputName { get; init; } = string.Empty;
    public string InputName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Analysis of command reuse from the library.
/// </summary>
public record CommandReuseAnalysis
{
    public int TotalCommands { get; init; }
    public int ReusedCommands { get; init; }
    public int NewCommands { get; init; }
    public double ReusePercentage { get; init; }
    public CommandReuse[] ReusedCommandDetails { get; init; } = Array.Empty<CommandReuse>();
    public CommandGap[] IdentifiedGaps { get; init; } = Array.Empty<CommandGap>();
}

/// <summary>
/// Details of a reused command.
/// </summary>
public record CommandReuse
{
    public string CommandId { get; init; } = string.Empty;
    public string CommandName { get; init; } = string.Empty;
    public int PreviousUseCount { get; init; }
    public string[] PreviousProjects { get; init; } = Array.Empty<string>();
    public string[] SimilarCommands { get; init; } = Array.Empty<string>();
    public double SimilarityScore { get; init; }
}

/// <summary>
/// Identified gap in the command library.
/// </summary>
public record CommandGap
{
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string[] SuggestedCommands { get; init; } = Array.Empty<string>();
    public string Priority { get; init; } = string.Empty;
}

/// <summary>
/// Cross-reference to documentation.
/// </summary>
public record DocumentationCrossReference
{
    public string CommandId { get; init; } = string.Empty;
    public string DocumentationSource { get; init; } = string.Empty;
    public string DocumentationUrl { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string[] RelatedTopics { get; init; } = Array.Empty<string>();
    public string[] CodeExamples { get; init; } = Array.Empty<string>();
}
