using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Base implementation for type values
/// </summary>
public abstract class BaseTypeValue : ITypeValue, IEquatable<BaseTypeValue>
{
    public string Name { get; }
    public string Description { get; }
    public int Value { get; }

    protected BaseTypeValue(string name, string description, int value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Value = value;
    }

    public virtual bool Equals(ITypeValue? other) => 
        other is BaseTypeValue baseOther && Equals(baseOther);

    public virtual bool Equals(BaseTypeValue? other) => 
        other != null && GetType() == other.GetType() && Value == other.Value;

    public override bool Equals(object? obj) => 
        obj is BaseTypeValue other && Equals(other);

    public override int GetHashCode() => 
        HashCode.Combine(GetType(), Value);

    public override string ToString() => Name;

    public static bool operator ==(BaseTypeValue? left, BaseTypeValue? right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

    public static bool operator !=(BaseTypeValue? left, BaseTypeValue? right) => 
        !(left == right);
}

/// <summary>
/// Base implementation for comparable type values
/// </summary>
public abstract class BaseComparableTypeValue : BaseTypeValue, IComparableTypeValue
{
    protected BaseComparableTypeValue(string name, string description, int value) 
        : base(name, description, value)
    {
    }

    public virtual int CompareTo(IComparableTypeValue? other) => 
        other is BaseComparableTypeValue baseOther ? Value.CompareTo(baseOther.Value) : 0;
}

/// <summary>
/// Base implementation for parsable type values
/// </summary>
public abstract class BaseParsableTypeValue : BaseTypeValue
{
    protected BaseParsableTypeValue(string name, string description, int value) 
        : base(name, description, value)
    {
    }

    // Derived classes must implement IParsableTypeValue directly
}

/// <summary>
/// Base implementation for serializable type values
/// </summary>
public abstract class BaseSerializableTypeValue : BaseTypeValue
{
    protected BaseSerializableTypeValue(string name, string description, int value) 
        : base(name, description, value)
    {
    }

    public virtual object ToSerializable() => new { Name, Description, Value };
    
    // Derived classes must implement ISerializableTypeValue directly
}
