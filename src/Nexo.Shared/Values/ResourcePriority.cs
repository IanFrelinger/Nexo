using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents resource priority as a value object
    /// </summary>
    public sealed class ResourcePriority : IEquatable<ResourcePriority>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ResourcePriority(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each resource priority
        public static readonly ResourcePriority Low = new("Low", "Low priority resource", 0);
        public static readonly ResourcePriority Normal = new("Normal", "Normal priority resource", 1);
        public static readonly ResourcePriority High = new("High", "High priority resource", 2);
        public static readonly ResourcePriority Critical = new("Critical", "Critical priority resource", 3);

        // Collection of all resource priorities
        public static readonly IReadOnlyList<ResourcePriority> All = new[]
        {
            Low, Normal, High, Critical
        };

        // Factory methods
        public static ResourcePriority FromName(string name) => 
            All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Normal;

        public static ResourcePriority FromValue(int value) => 
            All.FirstOrDefault(p => p.Value == value) ?? Normal;

        // Equality and comparison
        public bool Equals(ResourcePriority? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ResourcePriority other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ResourcePriority? left, ResourcePriority? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ResourcePriority? left, ResourcePriority? right) => 
            !(left == right);
    }
}
