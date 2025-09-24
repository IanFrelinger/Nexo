using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public class NetworkFeatureDetector : INetworkFeatureDetector
    {
        private readonly ILogger<NetworkFeatureDetector> _logger;
        public NetworkFeatureDetector(ILogger<NetworkFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting Network capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { Http = true, WebSockets = true, Grpc = false });
        }
    }
}
