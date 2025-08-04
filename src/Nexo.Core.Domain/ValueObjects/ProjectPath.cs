using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class ProjectPath : IComparable<ProjectPath>, IEquatable<ProjectPath>
    {
        public string Value { get; }
        public ProjectPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project path cannot be empty", nameof(value));
            try
            {
                Value = Path.GetFullPath(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid project path: " + ex.Message, nameof(value), ex);
            }
        }
        public static implicit operator string(ProjectPath path) { return path.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(ProjectPath other) { return string.Compare(Value, other == null ? null : other.Value, StringComparison.Ordinal); }
        public ProjectPath Combine(params string[] paths)
        {
            var combined = Value;
            foreach (var p in paths) { combined = Path.Combine(combined, p); }
            return new ProjectPath(combined);
        }
        public string GetDirectoryName() { return Path.GetDirectoryName(Value); }
        public string GetFileName() { return Path.GetFileName(Value); }
        public override bool Equals(object obj)
        {
            var result = Equals(obj as ProjectPath);
            return result;
        }
        public bool Equals(ProjectPath other)
        {
            var result = (ReferenceEquals(this, other)) || (other != null && string.Equals(Value, other.Value, StringComparison.Ordinal));
            return result;
        }
        public override int GetHashCode()
        {
            var hash = Value.GetHashCode();
            return hash;
        }
        public static bool operator ==(ProjectPath left, ProjectPath right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(ProjectPath left, ProjectPath right) => !(left == right);
    }
} 