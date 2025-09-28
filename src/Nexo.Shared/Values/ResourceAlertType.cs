using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents resource alert type as a value object
    /// </summary>
    public sealed class ResourceAlertType : IEquatable<ResourceAlertType>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ResourceAlertType(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each alert type
        public static readonly ResourceAlertType Usage = new("Usage", "Resource usage alert", 0);
        public static readonly ResourceAlertType Performance = new("Performance", "Performance degradation alert", 1);
        public static readonly ResourceAlertType Availability = new("Availability", "Resource availability alert", 2);
        public static readonly ResourceAlertType Capacity = new("Capacity", "Resource capacity alert", 3);
        public static readonly ResourceAlertType Error = new("Error", "Resource error alert", 4);

        // Collection of all alert types
        public static readonly IReadOnlyList<ResourceAlertType> All = new[]
        {
            Usage, Performance, Availability, Capacity, Error
        };

        // Factory methods
        public static ResourceAlertType FromName(string name) => 
            All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Usage;

        public static ResourceAlertType FromValue(int value) => 
            All.FirstOrDefault(t => t.Value == value) ?? Usage;

        // Equality and comparison
        public bool Equals(ResourceAlertType? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ResourceAlertType other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ResourceAlertType? left, ResourceAlertType? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ResourceAlertType? left, ResourceAlertType? right) => 
            !(left == right);
    }
}
