namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Thrown when self-adaptation attempts to modify a component in the immutable core.
/// The immutable core (observation pipeline, analysis, validation, inheritance, etc.)
/// must never be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreViolationException : DomainException
{
    public string? TargetComponent { get; }

    public ImmutableCoreViolationException(string message) : base(message)
    {
    }

    public ImmutableCoreViolationException(string message, string targetComponent) : base(message)
    {
        TargetComponent = targetComponent;
    }

    public ImmutableCoreViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
