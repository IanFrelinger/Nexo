using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents beta program status as a value object.
    /// 
    /// Provides predefined beta program status values:
    /// - Pending: Beta program is pending
    /// - Active: Beta program is active
    /// - Paused: Beta program is paused
    /// - Completed: Beta program has been completed
    /// - Cancelled: Beta program has been cancelled
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// </summary>
    public sealed class BetaProgramStatus : IEquatable<BetaProgramStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private BetaProgramStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each betaprogramstatus
        public static readonly BetaProgramStatus Pending = new("Pending", "Description for Pending", 0);
        public static readonly BetaProgramStatus Active = new("Active", "Description for Active", 1);
        public static readonly BetaProgramStatus Paused = new("Paused", "Description for Paused", 2);
        public static readonly BetaProgramStatus Completed = new("Completed", "Description for Completed", 3);
        public static readonly BetaProgramStatus Cancelled = new("Cancelled", "Description for Cancelled", 4);

        // Collection of all betaprogramstatuss
        public static readonly IReadOnlyList<BetaProgramStatus> All = new[]
        {
            Pending,
            Active,
            Paused,
            Completed,
            Cancelled,
        };

        // Factory methods
        public static BetaProgramStatus FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Pending;

        public static BetaProgramStatus FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Pending;

        // Equality and comparison
        public bool Equals(BetaProgramStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is BetaProgramStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(BetaProgramStatus? left, BetaProgramStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(BetaProgramStatus? left, BetaProgramStatus? right) => 
            !(left == right);
    }
}