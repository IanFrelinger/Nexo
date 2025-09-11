using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Base class for value objects
    /// </summary>
    public abstract class ValueObject
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsImmutable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
