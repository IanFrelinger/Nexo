using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class AgentRole : IComparable<AgentRole>, IEquatable<AgentRole>
    {
        private static readonly HashSet<string> StandardRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Manager", "Architect", "Developer", "Tester", "DevOps", "Designer", "Analyst"
        };
        public string Value { get; }
        public AgentRole(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Agent role cannot be empty", nameof(value));
            Value = value.Trim();
        }
        public bool IsStandardRole { get { return StandardRoles.Contains(Value); } }
        public static AgentRole Manager { get { return new AgentRole("Manager"); } }
        public static AgentRole Architect { get { return new AgentRole("Architect"); } }
        public static AgentRole Developer { get { return new AgentRole("Developer"); } }
        public static AgentRole Tester { get { return new AgentRole("Tester"); } }
        public static implicit operator string(AgentRole role) { return role.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(AgentRole other) { return string.Compare(Value, other == null ? null : other.Value, StringComparison.Ordinal); }
        public override bool Equals(object obj)
        {
            var result = Equals(obj as AgentRole);
            return result;
        }
        public bool Equals(AgentRole other)
        {
            var result = (ReferenceEquals(this, other)) || (other != null && string.Equals(Value, other.Value, StringComparison.Ordinal));
            return result;
        }
        public override int GetHashCode()
        {
            var hash = Value.GetHashCode();
            return hash;
        }
        public static bool operator ==(AgentRole left, AgentRole right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(AgentRole left, AgentRole right) => !(left == right);
    }
} 