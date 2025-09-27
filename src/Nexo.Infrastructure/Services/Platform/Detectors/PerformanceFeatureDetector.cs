using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public partial class PerformanceFeatureDetector : IPerformanceFeatureDetector
    {
        private readonly ILogger<PerformanceFeatureDetector> _logger;
        public PerformanceFeatureDetector(ILogger<PerformanceFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting Performance capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { Profiling = true, Metrics = true, TuningProfiles = true });
        }
    }
}
