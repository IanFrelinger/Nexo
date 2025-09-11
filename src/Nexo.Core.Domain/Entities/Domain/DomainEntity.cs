using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Base class for domain entities
    /// </summary>
    public abstract class DomainEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
    }
}
