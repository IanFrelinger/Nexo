using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Common
{
    /// <summary>
    /// Base class for all domain entities following DDD principles
    /// </summary>
    public abstract class BaseEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Base class for aggregate roots
    /// </summary>
    public abstract class AggregateRoot : BaseEntity
    {
        public List<DomainEvent> DomainEvents { get; set; } = new();
        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Base class for value objects
    /// </summary>
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();
        
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Base class for domain events
    /// </summary>
    public abstract class DomainEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
