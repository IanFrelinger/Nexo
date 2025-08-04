namespace Nexo.Feature.Analysis.Interfaces
{
    public interface IArchitectureAnalyzer : IAnalyzer
    {
        string AnalyzeArchitecture(string code);
    }
} 