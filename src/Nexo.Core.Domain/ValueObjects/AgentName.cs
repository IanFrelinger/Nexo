using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents a human-readable name for an agent
    /// </summary>
    public sealed class AgentName : IEquatable<AgentName>
    {
        public string Value { get; }

        public AgentName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(value));
            
            if (value.Length > 100)
                throw new ArgumentException("Agent name cannot exceed 100 characters", nameof(value));
            
            Value = value;
        }

        // Factory methods
        public static AgentName FromString(string value) => new(value);
        public static AgentName Create(string value) => new(value);

        // Equality and comparison
        public bool Equals(AgentName? other) => 
            other != null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => 
            obj is AgentName other && Equals(other);

        public override int GetHashCode() => 
            Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Value;

        public static bool operator ==(AgentName? left, AgentName? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        public static bool operator !=(AgentName? left, AgentName? right) => 
            !(left == right);

        public static implicit operator string(AgentName agentName) => agentName.Value;
        public static implicit operator AgentName(string value) => new(value);
    }
}