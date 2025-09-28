using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents the current operational status of an agent as a value object
    /// </summary>
    public sealed class AgentStatus : IEquatable<AgentStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private AgentStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
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

        // Equality and comparison
        public bool Equals(AgentStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is AgentStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(AgentStatus? left, AgentStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(AgentStatus? left, AgentStatus? right) => 
            !(left == right);
    }
}
