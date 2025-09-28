using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents operation types as a type value
/// </summary>
public sealed class OperationType : BaseTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private OperationType(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each operation type
    public static readonly OperationType Create = new("Create", "Create operation", 0);
    public static readonly OperationType Read = new("Read", "Read operation", 1);
    public static readonly OperationType Update = new("Update", "Update operation", 2);
    public static readonly OperationType Delete = new("Delete", "Delete operation", 3);
    public static readonly OperationType Execute = new("Execute", "Execute operation", 4);
    public static readonly OperationType Validate = new("Validate", "Validate operation", 5);
    public static readonly OperationType Process = new("Process", "Process operation", 6);
    public static readonly OperationType Initialize = new("Initialize", "Initialize operation", 7);
    public static readonly OperationType Shutdown = new("Shutdown", "Shutdown operation", 8);

    // Collection of all operation types
    public static readonly IReadOnlyList<OperationType> All = new[]
    {
        Create, Read, Update, Delete, Execute, Validate, Process, Initialize, Shutdown
    };

    // Factory methods
    public static OperationType FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Read;

    public static OperationType FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Read;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var operationType = FromName(value);
        result = operationType;
        return operationType != Read || value.Equals("Read", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid operation type: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "OperationType" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Read;
    }
}
