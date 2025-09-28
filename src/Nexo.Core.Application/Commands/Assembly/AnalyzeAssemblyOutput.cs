using Nexo.Shared.Models.Assembly;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Output for analyzing an assembly
    /// </summary>
    public class AnalyzeAssemblyOutput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public AssemblyMetadata Metadata { get; set; } = new AssemblyMetadata();
        public AssemblyAnalysisResult AnalysisResult { get; set; } = new AssemblyAnalysisResult();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
