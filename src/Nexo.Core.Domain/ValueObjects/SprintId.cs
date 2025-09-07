using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class SprintId : IComparable<SprintId>
    {
        public Guid Value { get; }
        public SprintId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Sprint ID cannot be empty", nameof(value));
            Value = value;
        }
        public static SprintId New() { return new SprintId(Guid.NewGuid()); }
        public static implicit operator Guid(SprintId id) { return id.Value; }
        public override string ToString() { return Value.ToString(); }
        public int CompareTo(SprintId? other) { return other == null ? 1 : Value.CompareTo(other.Value); }
    }
} 