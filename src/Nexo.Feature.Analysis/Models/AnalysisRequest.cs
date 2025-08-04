using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    public class AnalysisRequest
    {
        public string Code { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
        // Stub methods to resolve method group errors in tests
        public string TargetPathMethod() => TargetPath;
        public string AnalysisTypeMethod() => AnalysisType;
    }
} 