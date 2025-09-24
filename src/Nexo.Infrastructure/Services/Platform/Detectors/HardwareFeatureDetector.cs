using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public class HardwareFeatureDetector : IHardwareFeatureDetector
    {
        private readonly ILogger<HardwareFeatureDetector> _logger;
        public HardwareFeatureDetector(ILogger<HardwareFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting Hardware capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { CPU = "x64", GPU = true, Sensors = false });
        }
    }
}
