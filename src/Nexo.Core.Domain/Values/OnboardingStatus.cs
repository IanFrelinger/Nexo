using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents onboarding status as a value object.
    /// 
    /// Provides predefined onboarding status values:
    /// - Pending: Onboarding is pending
    /// - InProgress: Onboarding is in progress
    /// - Completed: Onboarding has been completed
    /// - Failed: Onboarding has failed
    /// - Cancelled: Onboarding has been cancelled
    /// - Paused: Onboarding is paused
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// </summary>
    public sealed class OnboardingStatus : IEquatable<OnboardingStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private OnboardingStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each onboardingstatus
        public static readonly OnboardingStatus Pending = new("Pending", "Description for Pending", 0);
        public static readonly OnboardingStatus InProgress = new("InProgress", "Description for InProgress", 1);
        public static readonly OnboardingStatus Completed = new("Completed", "Description for Completed", 2);
        public static readonly OnboardingStatus Failed = new("Failed", "Description for Failed", 3);
        public static readonly OnboardingStatus Cancelled = new("Cancelled", "Description for Cancelled", 4);
        public static readonly OnboardingStatus Paused = new("Paused", "Description for Paused", 5);

        // Collection of all onboardingstatuss
        public static readonly IReadOnlyList<OnboardingStatus> All = new[]
        {
            Pending,
            InProgress,
            Completed,
            Failed,
            Cancelled,
            Paused,
        };

        // Factory methods
        public static OnboardingStatus FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Pending;

        public static OnboardingStatus FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Pending;

        // Equality and comparison
        public bool Equals(OnboardingStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is OnboardingStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(OnboardingStatus? left, OnboardingStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(OnboardingStatus? left, OnboardingStatus? right) => 
            !(left == right);
    }
}