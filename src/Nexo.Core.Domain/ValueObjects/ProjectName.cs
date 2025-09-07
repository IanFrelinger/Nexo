using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class ProjectName : IComparable<ProjectName>, IEquatable<ProjectName>
    {
        private const int MinLength = 1;
        private const int MaxLength = 100;
        private static readonly Regex ValidNamePattern = new Regex(@"^[a-zA-Z][a-zA-Z0-9\-_]*$", RegexOptions.Compiled);
        public string Value { get; }
        public ProjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project name cannot be empty", nameof(value));
            if (value.Length < MinLength || value.Length > MaxLength)
                throw new ArgumentException(string.Format("Project name must be between {0} and {1} characters", MinLength, MaxLength), nameof(value));
            if (!ValidNamePattern.IsMatch(value))
                throw new ArgumentException("Project name must start with a letter and contain only letters, numbers, hyphens, and underscores", nameof(value));
            Value = value;
        }
        public static implicit operator string(ProjectName name) { return name.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(ProjectName? other) { return string.Compare(Value, other?.Value, StringComparison.Ordinal); }
        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length >= MinLength && value.Length <= MaxLength &&
                   ValidNamePattern.IsMatch(value);
        }
        public override bool Equals(object? obj)
        {
            var result = Equals(obj as ProjectName);
            return result;
        }
        public bool Equals(ProjectName? other)
        {
            var result = (ReferenceEquals(this, other)) || (other != null && string.Equals(Value, other.Value, StringComparison.Ordinal));
            return result;
        }
        public override int GetHashCode()
        {
            var hash = Value.GetHashCode();
            return hash;
        }
        public static bool operator ==(ProjectName left, ProjectName right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(ProjectName left, ProjectName right) => !(left == right);
    }
}