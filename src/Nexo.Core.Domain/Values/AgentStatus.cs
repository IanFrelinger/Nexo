using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents the current operational status of an agent as a value object
    /// </summary>
    public sealed class AgentStatus : BaseComparableTypeValue, IParsableTypeValue, ISerializableTypeValue
    {
        private AgentStatus(string name, string description, int value) : base(name, description, value)
        {
        }

        // Static instances for each agent status
        public static readonly AgentStatus Inactive = new("Inactive", "Agent is not active", 0);
        public static readonly AgentStatus Active = new("Active", "Agent is active and available for work", 1);
        public static readonly AgentStatus Busy = new("Busy", "Agent is currently busy with a task", 2);
        public static readonly AgentStatus Failed = new("Failed", "Agent has encountered a failure", 3);

        // Collection of all agent statuses
        public static readonly IReadOnlyList<AgentStatus> All = new[]
        {
            Inactive, Active, Busy, Failed
        };

        // Factory methods
        public static AgentStatus FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Inactive;

        public static AgentStatus FromValue(int value) => 
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
            throw new ArgumentException($"Invalid agent status: {value}", nameof(value));
        }

    // ISerializableTypeValue implementation
    public object ToSerializable() => new { Name, Description, Value, Type = "AgentStatus" };

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
}
