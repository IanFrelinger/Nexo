using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents log levels as a type value
/// </summary>
public sealed class LogLevel : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private LogLevel(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each log level
    public static readonly LogLevel Trace = new("Trace", "Trace level logging", 0);
    public static readonly LogLevel Debug = new("Debug", "Debug level logging", 1);
    public static readonly LogLevel Information = new("Information", "Information level logging", 2);
    public static readonly LogLevel Warning = new("Warning", "Warning level logging", 3);
    public static readonly LogLevel Error = new("Error", "Error level logging", 4);
    public static readonly LogLevel Critical = new("Critical", "Critical level logging", 5);

    // Collection of all log levels
    public static readonly IReadOnlyList<LogLevel> All = new[]
    {
        Trace, Debug, Information, Warning, Error, Critical
    };

    // Factory methods
    public static LogLevel FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Information;

    public static LogLevel FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Information;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var level = FromName(value);
        result = level;
        return level != Information || value.Equals("Information", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid log level: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "LogLevel" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Information;
    }
}