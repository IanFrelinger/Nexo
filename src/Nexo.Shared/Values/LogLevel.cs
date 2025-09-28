using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Defines logging severity levels as a value object
    /// </summary>
    public sealed class LogLevel : IEquatable<LogLevel>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private LogLevel(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each log level
        public static readonly LogLevel Trace = new("Trace", "Most detailed messages for debugging", 0);
        public static readonly LogLevel Debug = new("Debug", "Interactive investigation during development", 1);
        public static readonly LogLevel Information = new("Information", "General flow of the application", 2);
        public static readonly LogLevel Warning = new("Warning", "Abnormal or unexpected event", 3);
        public static readonly LogLevel Error = new("Error", "Current flow stopped due to failure", 4);
        public static readonly LogLevel Critical = new("Critical", "Unrecoverable application or system crash", 5);
        public static readonly LogLevel None = new("None", "No logging messages", 6);

        // Collection of all log levels
        public static readonly IReadOnlyList<LogLevel> All = new[]
        {
            Trace, Debug, Information, Warning, Error, Critical, None
        };

        // Factory methods
        public static LogLevel FromName(string name) => 
            All.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Information;

        public static LogLevel FromValue(int value) => 
            All.FirstOrDefault(l => l.Value == value) ?? Information;

        // Equality and comparison
        public bool Equals(LogLevel? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is LogLevel other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(LogLevel? left, LogLevel? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(LogLevel? left, LogLevel? right) => 
            !(left == right);
    }
}
