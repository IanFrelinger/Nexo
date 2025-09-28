using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents a unique identifier for an agent
    /// </summary>
    public sealed class AgentId : IEquatable<AgentId>
    {
        public string Value { get; }

        public AgentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(value));
            
            Value = value;
        }

        // Special agent IDs
        public static readonly AgentId Broadcast = new("BROADCAST");
        public static readonly AgentId System = new("SYSTEM");
        public static readonly AgentId Orchestrator = new("ORCHESTRATOR");

        // Factory methods
        public static AgentId NewId() => new(Guid.NewGuid().ToString());
        public static AgentId FromGuid(Guid guid) => new(guid.ToString());
        public static AgentId FromString(string value) => new(value);

        // Equality and comparison
        public bool Equals(AgentId? other) => 
            other != null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => 
            obj is AgentId other && Equals(other);

        public override int GetHashCode() => 
            Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Value;

        public static bool operator ==(AgentId? left, AgentId? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        public static bool operator !=(AgentId? left, AgentId? right) => 
            !(left == right);

        public static implicit operator string(AgentId agentId) => agentId.Value;
        public static implicit operator AgentId(string value) => new(value);
    }
}