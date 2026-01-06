using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents health status as a value object.
    /// 
    /// Provides predefined health status values:
    /// - Critical: Critical health status
    /// - Poor: Poor health status
    /// - Fair: Fair health status
    /// - Good: Good health status
    /// - Excellent: Excellent health status
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// </summary>
    public sealed class HealthStatus : IEquatable<HealthStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private HealthStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each healthstatus
        public static readonly HealthStatus Critical = new("Critical", "Description for Critical", 0);
        public static readonly HealthStatus Poor = new("Poor", "Description for Poor", 1);
        public static readonly HealthStatus Fair = new("Fair", "Description for Fair", 2);
        public static readonly HealthStatus Good = new("Good", "Description for Good", 3);
        public static readonly HealthStatus Excellent = new("Excellent", "Description for Excellent", 4);

        // Collection of all healthstatuss
        public static readonly IReadOnlyList<HealthStatus> All = new[]
        {
            Critical,
            Poor,
            Fair,
            Good,
            Excellent,
        };

        // Factory methods
        public static HealthStatus FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Critical;

        public static HealthStatus FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Critical;

        // Equality and comparison
        public bool Equals(HealthStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is HealthStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(HealthStatus? left, HealthStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(HealthStatus? left, HealthStatus? right) => 
            !(left == right);
    }
}