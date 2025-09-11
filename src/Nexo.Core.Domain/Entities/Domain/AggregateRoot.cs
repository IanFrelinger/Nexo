using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Represents an aggregate root
    /// </summary>
    public class AggregateRoot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<DomainEntity> Entities { get; set; } = new List<DomainEntity>();
        public List<ValueObject> ValueObjects { get; set; } = new List<ValueObject>();
        public List<DomainEvent> Events { get; set; } = new List<DomainEvent>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
