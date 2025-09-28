using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Values;

/// <summary>
/// Represents error types as a type value
/// </summary>
public sealed class ErrorType : BaseTypeValue, IParsableTypeValue, ISerializableTypeValue
{
    private ErrorType(string name, string description, int value) : base(name, description, value)
    {
    }

    // Static instances for each error type
    public static readonly ErrorType Validation = new("Validation", "Validation error", 0);
    public static readonly ErrorType Business = new("Business", "Business logic error", 1);
    public static readonly ErrorType System = new("System", "System error", 2);
    public static readonly ErrorType Network = new("Network", "Network error", 3);
    public static readonly ErrorType Timeout = new("Timeout", "Timeout error", 4);
    public static readonly ErrorType Authentication = new("Authentication", "Authentication error", 5);
    public static readonly ErrorType Authorization = new("Authorization", "Authorization error", 6);
    public static readonly ErrorType NotFound = new("NotFound", "Not found error", 7);
    public static readonly ErrorType Conflict = new("Conflict", "Conflict error", 8);
    public static readonly ErrorType Internal = new("Internal", "Internal error", 9);

    // Collection of all error types
    public static readonly IReadOnlyList<ErrorType> All = new[]
    {
        Validation, Business, System, Network, Timeout, Authentication, Authorization, NotFound, Conflict, Internal
    };

    // Factory methods
    public static ErrorType FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Internal;

    public static ErrorType FromValue(int value) => 
        All.FirstOrDefault(s => s.Value == value) ?? Internal;

    // IParsableTypeValue implementation
    public static bool TryParse(string value, out ITypeValue? result)
    {
        var errorType = FromName(value);
        result = errorType;
        return errorType != Internal || value.Equals("Internal", StringComparison.OrdinalIgnoreCase);
    }

    public static ITypeValue Parse(string value)
    {
        if (TryParse(value, out var result))
            return result;
        throw new ArgumentException($"Invalid error type: {value}", nameof(value));
    }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "ErrorType" };

    public static ITypeValue FromSerializable(object value)
    {
        if (value is { } obj)
        {
            var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(name))
                return FromName(name);
        }
        return Internal;
    }
}
