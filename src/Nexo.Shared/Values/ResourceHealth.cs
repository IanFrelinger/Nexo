using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents resource health status as a value object
    /// </summary>
    public sealed class ResourceHealth : IEquatable<ResourceHealth>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ResourceHealth(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each health status
        public static readonly ResourceHealth Healthy = new("Healthy", "Resource is healthy", 0);
        public static readonly ResourceHealth Warning = new("Warning", "Resource has warnings", 1);
        public static readonly ResourceHealth Degraded = new("Degraded", "Resource is degraded", 2);
        public static readonly ResourceHealth Critical = new("Critical", "Resource is critical", 3);
        public static readonly ResourceHealth Unknown = new("Unknown", "Resource health unknown", 4);

        // Collection of all health statuses
        public static readonly IReadOnlyList<ResourceHealth> All = new[]
        {
            Healthy, Warning, Degraded, Critical, Unknown
        };

        // Factory methods
        public static ResourceHealth FromName(string name) => 
            All.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Unknown;

        public static ResourceHealth FromValue(int value) => 
            All.FirstOrDefault(h => h.Value == value) ?? Unknown;

        // Equality and comparison
        public bool Equals(ResourceHealth? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ResourceHealth other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ResourceHealth? left, ResourceHealth? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ResourceHealth? left, ResourceHealth? right) => 
            !(left == right);
    }
}
