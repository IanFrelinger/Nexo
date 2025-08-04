namespace Nexo.Feature.Analysis.Models
{
    public class AnalysisIssue
    {
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        // Stub method to resolve method group error in tests
        public string SeverityMethod() => Severity;
    }
} 