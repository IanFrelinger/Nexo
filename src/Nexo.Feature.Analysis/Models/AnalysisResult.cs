using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    public class AnalysisResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AnalysisIssue> Issues { get; set; } = new List<AnalysisIssue>();
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        public string Summary { get; set; } = string.Empty;
        // Stub method to resolve method group error in tests
        public object MetricsMethod() => Metrics;
    }
} 