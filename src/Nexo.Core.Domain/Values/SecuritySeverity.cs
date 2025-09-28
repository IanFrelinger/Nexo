using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents security severity levels as a type value
    /// </summary>
    public sealed class SecuritySeverity : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
    {
        private SecuritySeverity(string name, string description, int value) : base(name, description, value)
        {
        }

        // Static instances for each security severity
        public static readonly SecuritySeverity Low = new("Low", "Low security severity", 0);
        public static readonly SecuritySeverity Medium = new("Medium", "Medium security severity", 1);
        public static readonly SecuritySeverity High = new("High", "High security severity", 2);
        public static readonly SecuritySeverity Critical = new("Critical", "Critical security severity", 3);

        // Collection of all security severities
        public static readonly IReadOnlyList<SecuritySeverity> All = new[]
        {
            Low, Medium, High, Critical
        };

        // Factory methods
        public static SecuritySeverity FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Low;

        public static SecuritySeverity FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Low;

        // IParsableTypeValue implementation
        public static bool TryParse(string value, out ITypeValue? result)
        {
            var severity = FromName(value);
            result = severity;
            return severity != Low || value.Equals("Low", StringComparison.OrdinalIgnoreCase);
        }

        public static ITypeValue Parse(string value)
        {
            if (TryParse(value, out var result))
                return result;
            throw new ArgumentException($"Invalid security severity: {value}", nameof(value));
        }

        // ISerializableTypeValue implementation
        public object ToSerializable() => new { Name, Description, Value, Type = "SecuritySeverity" };

        public static ITypeValue FromSerializable(object value)
        {
            if (value is { } obj)
            {
                var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
                if (!string.IsNullOrEmpty(name))
                    return FromName(name);
            }
            return Low;
        }
    }
}
