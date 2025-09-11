using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Represents a domain event
    /// </summary>
    public class DomainEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
