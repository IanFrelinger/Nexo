using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents validation severity levels as a type value
/// </summary>
public sealed class ValidationSeverity : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private ValidationSeverity(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each validation severity
    public static readonly ValidationSeverity Info = new("Info", "Informational validation message", 0);
    public static readonly ValidationSeverity Warning = new("Warning", "Warning validation message", 1);
    public static readonly ValidationSeverity Error = new("Error", "Error validation message", 2);
    public static readonly ValidationSeverity Critical = new("Critical", "Critical validation message", 3);

    // Collection of all validation severities
    public static readonly IReadOnlyList<ValidationSeverity> All = new[]
    {
        Info, Warning, Error, Critical
    };

    // Factory methods
    public static ValidationSeverity FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Info;

    public static ValidationSeverity FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Info;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var severity = FromName(value);
        result = severity;
        return severity != Info || value.Equals("Info", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid validation severity: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "ValidationSeverity" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Info;
    }
}