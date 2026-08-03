namespace Nexo.Core.Application.Generation;

/// <summary>Bounds for <see cref="GenerationRepairLoop{TArtifact}"/>.</summary>
/// <param name="MaxAttempts">Maximum draft→validate cycles (must be ≥ 1).</param>
public sealed record RepairOptions(int MaxAttempts = 3)
{
    /// <summary>Default: three draft attempts.</summary>
    public static RepairOptions Default { get; } = new(3);
}
