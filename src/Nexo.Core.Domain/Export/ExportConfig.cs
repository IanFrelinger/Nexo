namespace Nexo.Core.Domain.Export;

/// <summary>
/// Configuration for exporting a workflow.
/// </summary>
public class ExportConfig
{
    /// <summary>
    /// Export mode determines what gets generated.
    /// </summary>
    public ExportMode Mode { get; init; } = ExportMode.PureDeterministic;
    
    /// <summary>
    /// Target language/platform.
    /// </summary>
    public ExportTarget Target { get; init; } = ExportTarget.CSharp;
    
    /// <summary>
    /// For AIGeneratedThenDeterministic: which bricks should generate content.
    /// </summary>
    public IReadOnlyList<string>? GenerationBrickIds { get; init; }
    
    /// <summary>
    /// For AIGeneratedThenDeterministic: generation parameters.
    /// </summary>
    public GenerationConfig? GenerationConfig { get; init; }
    
    /// <summary>
    /// For WithRuntimeAI: include fallbacks?
    /// </summary>
    public bool IncludeFallbacks { get; init; } = true;
    
    /// <summary>
    /// Output configuration.
    /// </summary>
    public OutputConfig Output { get; init; } = new();
}

/// <summary>
/// Export mode determines what gets generated.
/// </summary>
public enum ExportMode
{
    /// <summary>
    /// Export as pure code with no AI dependencies.
    /// All agentic bricks are converted to their deterministic equivalents.
    /// </summary>
    PureDeterministic,
    
    /// <summary>
    /// Run AI NOW to generate content, then export as deterministic code.
    /// AI helps create data/logic at export time, but final code has no AI.
    /// </summary>
    AIGeneratedThenDeterministic,
    
    /// <summary>
    /// Export with full Nexo runtime, AI executes at runtime.
    /// </summary>
    WithRuntimeAI
}

/// <summary>
/// Target language/platform for export.
/// </summary>
public enum ExportTarget
{
    CSharp,
    Python,
    TypeScript,
    BehaviorTree,
    StateMachine   // Generic FSM format
}

/// <summary>
/// Configuration for AI generation during export.
/// </summary>
public class GenerationConfig
{
    /// <summary>
    /// How many variations to generate per item.
    /// </summary>
    public int VariationsPerItem { get; init; } = 10;
    
    /// <summary>
    /// Provider to use for generation.
    /// </summary>
    public string Provider { get; init; } = "openai";
    
    /// <summary>
    /// Model to use.
    /// </summary>
    public string Model { get; init; } = "gpt-4";
    
    /// <summary>
    /// Human review required before export.
    /// </summary>
    public bool RequireReview { get; init; } = true;
}

/// <summary>
/// Output configuration for export.
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Output format.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.Project;
    
    /// <summary>
    /// Namespace for generated code.
    /// </summary>
    public string Namespace { get; init; } = "Generated";
    
    /// <summary>
    /// Include XML documentation.
    /// </summary>
    public bool IncludeDocumentation { get; init; } = true;
    
    /// <summary>
    /// Include unit tests.
    /// </summary>
    public bool IncludeTests { get; init; } = false;
}

/// <summary>
/// Output format for export.
/// </summary>
public enum OutputFormat
{
    Project,     // Full project structure with csproj/package.json
    Files,       // Individual code files
    SingleFile,  // Everything in one file
    Zip          // Downloadable archive
}

