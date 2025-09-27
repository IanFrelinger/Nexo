using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    /// <summary>
    /// Performance analyzer interface
    /// </summary>
    public interface IPerformanceAnalyzer
    {
        Task<PerformanceAnalysis> AnalyzeSnapshotAsync(GamePerformanceSnapshot snapshot);
    }
}
