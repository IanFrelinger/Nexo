namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Output for analyzing IL code
    /// </summary>
    public class AnalyzeILOutput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public object ILAnalysis { get; set; } = new object();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
