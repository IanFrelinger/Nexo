namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Validation criterion
/// </summary>
public record ValidationCriterion
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsMet { get; init; }
    public double Score { get; init; }
    public string? Issue { get; init; }
} 