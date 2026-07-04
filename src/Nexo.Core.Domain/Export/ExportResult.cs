namespace Nexo.Core.Domain.Export;

/// <summary>
/// Result of exporting a workflow.
/// </summary>
public class ExportResult
{
    /// <summary>Whether export completed without fatal errors.</summary>
    public bool Success { get; init; }

    /// <summary>Export mode used for this run.</summary>
    public ExportMode Mode { get; init; }

    /// <summary>Target language or artifact type generated.</summary>
    public ExportTarget Target { get; init; }
    
    /// <summary>
    /// Generated files.
    /// </summary>
    public IReadOnlyList<ExportedFile> Files { get; init; } = [];
    
    /// <summary>
    /// For AIGeneratedThenDeterministic: what content was generated.
    /// </summary>
    public GenerationSummary? GenerationSummary { get; init; }
    
    /// <summary>
    /// Runtime requirements (if any).
    /// </summary>
    public IReadOnlyList<string> RuntimeRequirements { get; init; } = [];
    
    /// <summary>
    /// Warnings or notes.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = [];
}
