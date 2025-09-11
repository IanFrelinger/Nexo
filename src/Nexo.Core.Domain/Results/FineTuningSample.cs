using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents a sample for fine-tuning AI models
    /// </summary>
    public class FineTuningSample
    {
        public string Id { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsValidated { get; set; }
        public string ValidationNotes { get; set; } = string.Empty;
    }
}
