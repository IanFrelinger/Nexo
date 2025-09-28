using System;

namespace Nexo.Shared.Interfaces.Types;

/// <summary>
/// Base interface for type values that can be implemented as structs, classes, etc.
/// </summary>
public interface ITypeValue : IEquatable<ITypeValue>
{
    /// <summary>
    /// The name of the type value
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The description of the type value
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// The integer value representation
    /// </summary>
    int Value { get; }
}

/// <summary>
/// Interface for type values that can be compared
/// </summary>
public interface IComparableTypeValue : ITypeValue, IComparable<IComparableTypeValue>
{
}

/// <summary>
/// Interface for type values that support parsing from strings
/// </summary>
public interface IParsableTypeValue : ITypeValue
{
    /// <summary>
    /// Try to parse a string into a type value
    /// </summary>
    static abstract bool TryParse(string value, out ITypeValue? result);
    
    /// <summary>
    /// Parse a string into a type value, throwing if invalid
    /// </summary>
    static abstract ITypeValue Parse(string value);
}

/// <summary>
/// Interface for type values that can be serialized
/// </summary>
public interface ISerializableTypeValue : ITypeValue
{
    /// <summary>
    /// Convert to a serializable representation
    /// </summary>
    object ToSerializable();
    
    /// <summary>
    /// Create from a serializable representation
    /// </summary>
    static abstract ITypeValue FromSerializable(object value);
}
