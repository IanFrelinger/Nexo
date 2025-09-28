using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents resource type as a value object
    /// </summary>
    public sealed class ResourceType : IEquatable<ResourceType>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ResourceType(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each resource type
        public static readonly ResourceType CPU = new("CPU", "Central Processing Unit", 0);
        public static readonly ResourceType Memory = new("Memory", "System Memory", 1);
        public static readonly ResourceType Storage = new("Storage", "Storage Space", 2);
        public static readonly ResourceType Network = new("Network", "Network Bandwidth", 3);
        public static readonly ResourceType GPU = new("GPU", "Graphics Processing Unit", 4);
        public static readonly ResourceType Database = new("Database", "Database Connection", 5);

        // Collection of all resource types
        public static readonly IReadOnlyList<ResourceType> All = new[]
        {
            CPU, Memory, Storage, Network, GPU, Database
        };

        // Factory methods
        public static ResourceType FromName(string name) => 
            All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? CPU;

        public static ResourceType FromValue(int value) => 
            All.FirstOrDefault(t => t.Value == value) ?? CPU;

        // Equality and comparison
        public bool Equals(ResourceType? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ResourceType other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ResourceType? left, ResourceType? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ResourceType? left, ResourceType? right) => 
            !(left == right);
    }
}
