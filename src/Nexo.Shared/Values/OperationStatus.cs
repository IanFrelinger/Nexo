using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents the status of an operation as a type value
/// </summary>
public sealed class OperationStatus : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private OperationStatus(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each operation status
    public static readonly OperationStatus Success = new("Success", "Operation completed successfully", 0);
    public static readonly OperationStatus Failure = new("Failure", "Operation failed", 1);
    public static readonly OperationStatus PartialSuccess = new("PartialSuccess", "Operation completed with partial success", 2);
    public static readonly OperationStatus Cancelled = new("Cancelled", "Operation was cancelled", 3);
    public static readonly OperationStatus Timeout = new("Timeout", "Operation timed out", 4);

    // Collection of all operation statuses
    public static readonly IReadOnlyList<OperationStatus> All = new[]
    {
        Success, Failure, PartialSuccess, Cancelled, Timeout
    };

    // Factory methods
    public static OperationStatus FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Failure;

    public static OperationStatus FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Failure;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var status = FromName(value);
        result = status;
        return status != Failure || value.Equals("Failure", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid operation status: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "OperationStatus" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Failure;
    }
}
