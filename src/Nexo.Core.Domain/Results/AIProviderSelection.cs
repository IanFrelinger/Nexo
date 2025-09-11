using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// AI provider selection result
    /// </summary>
    public class AIProviderSelection
    {
        /// <summary>
        /// Selected provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Selected provider name
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;
        
        /// <summary>
        /// Selected model ID
        /// </summary>
        public string ModelId { get; set; } = string.Empty;
        
        /// <summary>
        /// Selection reason
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Selection confidence score (0-1)
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// Selection criteria used
        /// </summary>
        public List<string> Criteria { get; set; } = new();
        
        /// <summary>
        /// Alternative providers considered
        /// </summary>
        public List<AlternativeProvider> Alternatives { get; set; } = new();
        
        /// <summary>
        /// Selection duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when selection was made
        /// </summary>
        public DateTime SelectedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    /// <summary>
    /// Alternative provider considered during selection
    /// </summary>
    public class AlternativeProvider
    {
        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Provider name
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;
        
        /// <summary>
        /// Model ID
        /// </summary>
        public string ModelId { get; set; } = string.Empty;
        
        /// <summary>
        /// Score for this provider
        /// </summary>
        public double Score { get; set; }
        
        /// <summary>
        /// Reason why this provider was not selected
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
