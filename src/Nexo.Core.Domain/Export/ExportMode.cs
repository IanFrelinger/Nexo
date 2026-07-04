namespace Nexo.Core.Domain.Export;

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
