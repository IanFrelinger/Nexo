using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents information about an AI provider
    /// </summary>
    public class AIProviderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public List<string> SupportedLanguages { get; set; } = new();
        public List<string> SupportedPlatforms { get; set; } = new();
        public ProviderStatus Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public enum ProviderStatus
    {
        Active,
        Inactive,
        Maintenance,
        Deprecated
    }
}
