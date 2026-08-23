namespace Ashlar.Core.Domain.Values;

/// <summary>
/// Interface for type value objects that provide value/display pairs.
/// 
/// Defines the contract for value objects that represent domain concepts
/// with both an internal value and a human-readable display name.
/// 
/// Used by BaseTypeValue and its derived classes to provide a consistent
/// pattern for domain value objects throughout the system.
/// </summary>
public interface ITypeValue
{
    /// <summary>Internal serialized value representation.</summary>
    string Value { get; }

    /// <summary>Human-readable display label.</summary>
    string Display { get; }
}
