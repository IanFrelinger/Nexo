using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Input for analyzing IL code
    /// </summary>
    public class AnalyzeILInput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public ILAnalysisOptions AnalysisOptions { get; set; } = new ILAnalysisOptions();
    }

    /// <summary>
    /// Options for IL analysis
    /// </summary>
    public class ILAnalysisOptions
    {
        public bool IncludeILCode { get; set; } = true;
        public bool IncludeOptimizations { get; set; } = true;
        public bool IncludePerformanceMetrics { get; set; } = false;
        public bool IncludeSecurityAnalysis { get; set; } = true;
        public bool IncludePatternAnalysis { get; set; } = true;
        public bool IncludeDependencyAnalysis { get; set; } = false;
        public Dictionary<string, object> AdditionalOptions { get; set; } = new Dictionary<string, object>();
    }
}
