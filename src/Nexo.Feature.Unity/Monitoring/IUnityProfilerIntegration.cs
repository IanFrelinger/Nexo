using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    /// <summary>
    /// Unity profiler integration interface
    /// </summary>
    public interface IUnityProfilerIntegration
    {
        Task StartProfilingAsync(UnityProfilingConfiguration configuration);
        Task StopProfilingAsync();
        Task<UnityProfilerData> GetCurrentProfilerDataAsync();
    }
}
