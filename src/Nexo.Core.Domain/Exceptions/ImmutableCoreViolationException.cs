namespace Nexo.Core.Domain.Exceptions;

/// <summary>
/// Thrown when self-adaptation attempts to modify a component in the immutable core.
/// The immutable core (observation pipeline, analysis, validation, inheritance, etc.)
/// must never be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreViolationException : DomainException
{
    /// <summary>Component or subsystem that self-adaptation attempted to modify.</summary>
    public string? TargetComponent { get; }

    /// <summary>Creates an immutable-core violation exception.</summary>
    public ImmutableCoreViolationException(string message) : base(message)
    {
    }

    /// <summary>Creates an immutable-core violation exception with a target component name.</summary>
    public ImmutableCoreViolationException(string message, string targetComponent) : base(message)
    {
        TargetComponent = targetComponent;
    }

    /// <summary>Creates an immutable-core violation exception with an inner cause.</summary>
    public ImmutableCoreViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
