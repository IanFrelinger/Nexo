using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ExtractedRequirement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}