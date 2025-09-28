using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents sprint status as a value object
    /// </summary>
    public sealed class SprintStatus : IEquatable<SprintStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private SprintStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each sprint status
        public static readonly SprintStatus Planning = new("Planning", "Sprint is in planning phase", 0);
        public static readonly SprintStatus Active = new("Active", "Sprint is currently active", 1);
        public static readonly SprintStatus Completed = new("Completed", "Sprint has been completed", 2);
        public static readonly SprintStatus Closed = new("Closed", "Sprint has been closed", 3);
        public static readonly SprintStatus Cancelled = new("Cancelled", "Sprint has been cancelled", 4);

        // Collection of all sprint statuses
        public static readonly IReadOnlyList<SprintStatus> All = new[]
        {
            Planning, Active, Completed, Closed, Cancelled
        };

        // Factory methods
        public static SprintStatus FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Planning;

        public static SprintStatus FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Planning;

        // Equality and comparison
        public bool Equals(SprintStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is SprintStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(SprintStatus? left, SprintStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(SprintStatus? left, SprintStatus? right) => 
            !(left == right);
    }
}
