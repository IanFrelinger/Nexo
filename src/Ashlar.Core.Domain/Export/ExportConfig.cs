namespace Ashlar.Core.Domain.Export;

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
