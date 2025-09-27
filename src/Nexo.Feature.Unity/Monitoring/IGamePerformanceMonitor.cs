using System;
using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    /// <summary>
    /// Game performance monitor interface
    /// </summary>
    public interface IGamePerformanceMonitor : IDisposable
    {
        Task StartMonitoringAsync(GameMonitoringConfiguration config);
        Task StopMonitoringAsync();
        Task<GamePerformanceReport> GeneratePerformanceReportAsync(TimeSpan timeRange);
        Task<GamePerformanceSnapshot> GetCurrentPerformanceSnapshotAsync();
    }
}
