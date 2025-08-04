using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class AgentId : IComparable<AgentId>
    {
        public string Value { get; }
        public AgentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Agent ID cannot be empty", nameof(value));
            Value = value;
        }
        public static AgentId New() { return new AgentId("agent-" + Guid.NewGuid().ToString()); }
        public static implicit operator string(AgentId id) { return id.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(AgentId other) { return string.Compare(Value, other == null ? null : other.Value, StringComparison.Ordinal); }
    }
} 