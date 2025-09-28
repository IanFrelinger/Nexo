using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents resource alert severity as a value object
    /// </summary>
    public sealed class ResourceAlertSeverity : IEquatable<ResourceAlertSeverity>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ResourceAlertSeverity(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each alert severity
        public static readonly ResourceAlertSeverity Info = new("Info", "Informational alert", 0);
        public static readonly ResourceAlertSeverity Warning = new("Warning", "Warning alert", 1);
        public static readonly ResourceAlertSeverity Error = new("Error", "Error alert", 2);
        public static readonly ResourceAlertSeverity Critical = new("Critical", "Critical alert", 3);

        // Collection of all alert severities
        public static readonly IReadOnlyList<ResourceAlertSeverity> All = new[]
        {
            Info, Warning, Error, Critical
        };

        // Factory methods
        public static ResourceAlertSeverity FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Info;

        public static ResourceAlertSeverity FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Info;

        // Equality and comparison
        public bool Equals(ResourceAlertSeverity? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ResourceAlertSeverity other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ResourceAlertSeverity? left, ResourceAlertSeverity? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ResourceAlertSeverity? left, ResourceAlertSeverity? right) => 
            !(left == right);
    }
}
