using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents AI engine type as a value object.
    /// 
    /// Provides predefined AI engine type values:
    /// - GPT: GPT-based engine
    /// - Claude: Claude-based engine
    /// - Gemini: Gemini-based engine
    /// - Llama: Llama-based engine
    /// - Custom: Custom engine
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// Used to identify the type of AI engine being used.
    /// </summary>
    public sealed class AIEngineType : IEquatable<AIEngineType>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private AIEngineType(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each engine type
        public static readonly AIEngineType GPT = new("GPT", "GPT-based engine", 0);
        public static readonly AIEngineType Claude = new("Claude", "Claude-based engine", 1);
        public static readonly AIEngineType Gemini = new("Gemini", "Gemini-based engine", 2);
        public static readonly AIEngineType Llama = new("Llama", "Llama-based engine", 3);
        public static readonly AIEngineType Custom = new("Custom", "Custom engine", 4);

        // Collection of all engine types
        public static readonly IReadOnlyList<AIEngineType> All = new[]
        {
            GPT, Claude, Gemini, Llama, Custom
        };

        // Factory methods
        public static AIEngineType FromName(string name) => 
            All.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? GPT;

        public static AIEngineType FromValue(int value) => 
            All.FirstOrDefault(e => e.Value == value) ?? GPT;

        // Equality and comparison
        public bool Equals(AIEngineType? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is AIEngineType other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(AIEngineType? left, AIEngineType? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(AIEngineType? left, AIEngineType? right) => 
            !(left == right);
    }
}
