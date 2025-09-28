using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents AI confidence level as a value object
    /// </summary>
    public sealed class AIConfidenceLevel : IEquatable<AIConfidenceLevel>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private AIConfidenceLevel(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each confidence level
        public static readonly AIConfidenceLevel VeryLow = new("VeryLow", "Very low confidence", 0);
        public static readonly AIConfidenceLevel Low = new("Low", "Low confidence", 1);
        public static readonly AIConfidenceLevel Medium = new("Medium", "Medium confidence", 2);
        public static readonly AIConfidenceLevel High = new("High", "High confidence", 3);
        public static readonly AIConfidenceLevel VeryHigh = new("VeryHigh", "Very high confidence", 4);

        // Collection of all confidence levels
        public static readonly IReadOnlyList<AIConfidenceLevel> All = new[]
        {
            VeryLow, Low, Medium, High, VeryHigh
        };

        // Factory methods
        public static AIConfidenceLevel FromName(string name) => 
            All.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Medium;

        public static AIConfidenceLevel FromValue(int value) => 
            All.FirstOrDefault(c => c.Value == value) ?? Medium;

        // Equality and comparison
        public bool Equals(AIConfidenceLevel? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is AIConfidenceLevel other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(AIConfidenceLevel? left, AIConfidenceLevel? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(AIConfidenceLevel? left, AIConfidenceLevel? right) => 
            !(left == right);
    }
}
