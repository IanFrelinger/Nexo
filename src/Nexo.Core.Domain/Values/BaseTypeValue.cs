namespace Nexo.Core.Domain.Values;

/// <summary>
/// Base class for type value objects that provide value/display pairs.
/// 
/// Provides a common implementation for value objects that need both:
/// - Value: The internal value representation (e.g., "idle", "low")
/// - Display: The human-readable display name (e.g., "Idle", "Low")
/// 
/// Derived classes (e.g., AgentStatus, SecuritySeverity) provide static instances
/// for each valid value. This pattern replaces enums with more flexible value objects.
/// 
/// Implements ITypeValue interface.
/// </summary>
public abstract class BaseTypeValue : ITypeValue
{
    /// <inheritdoc />
    public string Value { get; }

    /// <inheritdoc />
    public string Display { get; }

    /// <summary>Creates a value/display pair.</summary>
    /// <param name="value">Internal serialized value.</param>
    /// <param name="display">Human-readable label.</param>
    protected BaseTypeValue(string value, string display)
    {
        Value = value;
        Display = display;
    }
}
