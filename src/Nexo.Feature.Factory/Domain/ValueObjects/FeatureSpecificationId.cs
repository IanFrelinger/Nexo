using System;

namespace Nexo.Feature.Factory.Domain.ValueObjects
{
    /// <summary>
    /// Represents a unique identifier for a feature specification.
    /// </summary>
    public sealed class FeatureSpecificationId : IComparable<FeatureSpecificationId>, IEquatable<FeatureSpecificationId>
    {
        public string Value { get; }

        public FeatureSpecificationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Feature specification ID cannot be empty", nameof(value));
            Value = value;
        }

        public static FeatureSpecificationId New() => new FeatureSpecificationId($"spec-{Guid.NewGuid():N}");

        public static implicit operator string(FeatureSpecificationId? id) => id?.Value ?? string.Empty;

        public override string ToString() => Value;

        public int CompareTo(FeatureSpecificationId? other)
        {
            if (other == null) return 1;
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(FeatureSpecificationId? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as FeatureSpecificationId);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(FeatureSpecificationId? left, FeatureSpecificationId? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(FeatureSpecificationId? left, FeatureSpecificationId? right) => !(left == right);
    }
}
