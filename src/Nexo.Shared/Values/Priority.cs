using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents priority levels as a type value
/// </summary>
public sealed class Priority : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private Priority(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each priority level
    public static readonly Priority Low = new("Low", "Low priority", 0);
    public static readonly Priority Normal = new("Normal", "Normal priority", 1);
    public static readonly Priority High = new("High", "High priority", 2);
    public static readonly Priority Critical = new("Critical", "Critical priority", 3);
    public static readonly Priority Emergency = new("Emergency", "Emergency priority", 4);

    // Collection of all priorities
    public static readonly IReadOnlyList<Priority> All = new[]
    {
        Low, Normal, High, Critical, Emergency
    };

    // Factory methods
    public static Priority FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Normal;

    public static Priority FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Normal;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var priority = FromName(value);
        result = priority;
        return priority != Normal || value.Equals("Normal", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid priority: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "Priority" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Normal;
    }
}
