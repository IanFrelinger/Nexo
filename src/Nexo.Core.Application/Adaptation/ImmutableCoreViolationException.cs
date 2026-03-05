namespace Nexo.Core.Application.Adaptation;

/// <summary>
/// Thrown when self-adaptation attempts to target a component in the immutable core.
/// The immutable core (observation pipeline, analysis engine, validation, trust, rollback)
/// must never be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreViolationException : InvalidOperationException
{
    /// <summary>The path or component ID that was rejected.</summary>
    public string TargetPathOrComponentId { get; }

    public ImmutableCoreViolationException(string targetPathOrComponentId)
        : base($"Cannot adapt immutable core: {targetPathOrComponentId}")
    {
        TargetPathOrComponentId = targetPathOrComponentId ?? throw new ArgumentNullException(nameof(targetPathOrComponentId));
    }

    public ImmutableCoreViolationException(string targetPathOrComponentId, Exception innerException)
        : base($"Cannot adapt immutable core: {targetPathOrComponentId}", innerException)
    {
        TargetPathOrComponentId = targetPathOrComponentId ?? throw new ArgumentNullException(nameof(targetPathOrComponentId));
    }
}
