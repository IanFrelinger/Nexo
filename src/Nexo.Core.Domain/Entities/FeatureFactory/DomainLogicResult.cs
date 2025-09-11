using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class DomainLogicResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AggregateRoots { get; set; } = new();
        public List<string> DomainEvents { get; set; } = new();
        public List<string> Repositories { get; set; } = new();
        public List<string> Factories { get; set; } = new();
        public List<string> Specifications { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Generated";
    }
}