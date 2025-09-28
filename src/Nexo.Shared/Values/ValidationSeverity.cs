using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents validation severity as a value object
    /// </summary>
    public sealed class ValidationSeverity : IEquatable<ValidationSeverity>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ValidationSeverity(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each validation severity
        public static readonly ValidationSeverity Info = new("Info", "Informational validation message", 0);
        public static readonly ValidationSeverity Warning = new("Warning", "Warning validation message", 1);
        public static readonly ValidationSeverity Error = new("Error", "Error validation message", 2);
        public static readonly ValidationSeverity Critical = new("Critical", "Critical validation message", 3);

        // Collection of all validation severities
        public static readonly IReadOnlyList<ValidationSeverity> All = new[]
        {
            Info, Warning, Error, Critical
        };

        // Factory methods
        public static ValidationSeverity FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Info;

        public static ValidationSeverity FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Info;

        // Equality and comparison
        public bool Equals(ValidationSeverity? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ValidationSeverity other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ValidationSeverity? left, ValidationSeverity? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ValidationSeverity? left, ValidationSeverity? right) => 
            !(left == right);
    }
}
