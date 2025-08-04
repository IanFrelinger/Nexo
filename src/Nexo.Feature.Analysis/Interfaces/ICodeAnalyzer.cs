namespace Nexo.Feature.Analysis.Interfaces
{
    public interface ICodeAnalyzer : IAnalyzer
    {
        string AnalyzeCode(string code);
    }
} 