using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Represents information about an AI provider
    /// </summary>
    public class AIProviderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public List<string> SupportedModels { get; set; } = new List<string>();
        public List<string> Capabilities { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public bool IsAvailable { get; set; }
        public string Status { get; set; } = "Unknown";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public List<string> SupportedLanguages { get; set; } = new List<string>();
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
    }
}
