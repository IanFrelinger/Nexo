using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents risk level as a value object.
    /// 
    /// Provides predefined risk level values:
    /// - Low: Low risk level
    /// - Medium: Medium risk level
    /// - High: High risk level
    /// - Critical: Critical risk level
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// </summary>
    public sealed class RiskLevel : IEquatable<RiskLevel>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private RiskLevel(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each risklevel
        public static readonly RiskLevel Low = new("Low", "Description for Low", 1);
        public static readonly RiskLevel Medium = new("Medium", "Description for Medium", 2);
        public static readonly RiskLevel High = new("High", "Description for High", 3);
        public static readonly RiskLevel Critical = new("Critical", "Description for Critical", 4);

        // Collection of all risklevels
        public static readonly IReadOnlyList<RiskLevel> All = new[]
        {
            Low,
            Medium,
            High,
            Critical,
        };

        // Factory methods
        public static RiskLevel FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Low;

        public static RiskLevel FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Low;

        // Equality and comparison
        public bool Equals(RiskLevel? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is RiskLevel other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(RiskLevel? left, RiskLevel? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(RiskLevel? left, RiskLevel? right) => 
            !(left == right);
    }
}