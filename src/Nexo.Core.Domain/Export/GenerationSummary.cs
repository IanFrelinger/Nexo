namespace Nexo.Core.Domain.Export;

/// <summary>
/// Summary of AI generation during export.
/// </summary>
public class GenerationSummary
{
    /// <summary>Total distinct items generated during export.</summary>
    public int ItemsGenerated { get; init; }

    /// <summary>Total variations created across all generated items.</summary>
    public int VariationsCreated { get; init; }

    /// <summary>Per-item generation details.</summary>
    public IReadOnlyList<GeneratedItem> Items { get; init; } = [];
}
