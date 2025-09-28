using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents status values as a type value
/// </summary>
public sealed class Status : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private Status(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each status
    public static readonly Status Active = new("Active", "Active status", 0);
    public static readonly Status Inactive = new("Inactive", "Inactive status", 1);
    public static readonly Status Pending = new("Pending", "Pending status", 2);
    public static readonly Status Completed = new("Completed", "Completed status", 3);
    public static readonly Status Failed = new("Failed", "Failed status", 4);
    public static readonly Status Cancelled = new("Cancelled", "Cancelled status", 5);
    public static readonly Status Running = new("Running", "Running status", 6);
    public static readonly Status Stopped = new("Stopped", "Stopped status", 7);

    // Collection of all statuses
    public static readonly IReadOnlyList<Status> All = new[]
    {
        Active, Inactive, Pending, Completed, Failed, Cancelled, Running, Stopped
    };

    // Factory methods
    public static Status FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Inactive;

    public static Status FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Inactive;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var status = FromName(value);
        result = status;
        return status != Inactive || value.Equals("Inactive", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid status: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "Status" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Inactive;
    }
}
