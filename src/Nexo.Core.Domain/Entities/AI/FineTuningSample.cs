using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Represents a fine-tuning sample for AI model training
    /// </summary>
    public class FineTuningSample
    {
        public string Id { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public double Weight { get; set; } = 1.0;
        public bool IsValidated { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Quality { get; set; } = "Good";
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
