using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents method visibility as a value object.
    /// 
    /// Provides predefined method visibility values:
    /// - Public: Public method
    /// - Private: Private method
    /// - Protected: Protected method
    /// - Internal: Internal method
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// Used in code analysis and reflection operations.
    /// </summary>
    public sealed class MethodVisibility : IEquatable<MethodVisibility>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private MethodVisibility(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each method visibility
        public static readonly MethodVisibility Public = new("Public", "Public method", 0);
        public static readonly MethodVisibility Private = new("Private", "Private method", 1);
        public static readonly MethodVisibility Protected = new("Protected", "Protected method", 2);
        public static readonly MethodVisibility Internal = new("Internal", "Internal method", 3);

        // Collection of all method visibilities
        public static readonly IReadOnlyList<MethodVisibility> All = new[]
        {
            Public, Private, Protected, Internal
        };

        // Factory methods
        public static MethodVisibility FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Public;

        public static MethodVisibility FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Public;

        // Equality and comparison
        public bool Equals(MethodVisibility? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is MethodVisibility other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(MethodVisibility? left, MethodVisibility? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(MethodVisibility? left, MethodVisibility? right) => 
            !(left == right);
    }
}
