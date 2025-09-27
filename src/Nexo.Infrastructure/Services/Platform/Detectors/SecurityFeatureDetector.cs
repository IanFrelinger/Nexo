using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public partial class SecurityFeatureDetector : ISecurityFeatureDetector
    {
        private readonly ILogger<SecurityFeatureDetector> _logger;
        public SecurityFeatureDetector(ILogger<SecurityFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting Security capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { Auth = true, Permissions = true, Crypto = true });
        }
    }
}
