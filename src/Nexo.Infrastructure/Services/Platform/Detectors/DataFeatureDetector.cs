using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public class DataFeatureDetector : IDataFeatureDetector
    {
        private readonly ILogger<DataFeatureDetector> _logger;
        public DataFeatureDetector(ILogger<DataFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting Data capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { Sql = true, NoSql = true, Caching = true });
        }
    }
}
