using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class AgentName : IComparable<AgentName>, IEquatable<AgentName>
    {
        public string Value { get; }
        public AgentName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Agent name cannot be empty", nameof(value));
            if (value.Length > 100)
                throw new ArgumentException("Agent name cannot exceed 100 characters", nameof(value));
            Value = value.Trim();
        }
        public static implicit operator string(AgentName name) { return name.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(AgentName other) { return string.Compare(Value, other == null ? null : other.Value, StringComparison.Ordinal); }
        public override bool Equals(object obj)
        {
            var result = Equals(obj as AgentName);
            return result;
        }
        public bool Equals(AgentName other)
        {
            var result = (ReferenceEquals(this, other)) || (other != null && string.Equals(Value, other.Value, StringComparison.Ordinal));
            return result;
        }
        public override int GetHashCode()
        {
            var hash = Value.GetHashCode();
            return hash;
        }
        public static bool operator ==(AgentName left, AgentName right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(AgentName left, AgentName right) => !(left == right);
    }
}