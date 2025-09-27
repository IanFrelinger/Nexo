using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform
{
    public partial class UiFeatureDetector : IUiFeatureDetector
    {
        private readonly ILogger<UiFeatureDetector> _logger;
        public UiFeatureDetector(ILogger<UiFeatureDetector> logger) { _logger = logger; }
        public Task<object> DetectAsync(string platform, CancellationToken ct)
        {
            _logger.LogInformation("Detecting UI capabilities for {Platform}", platform);
            return Task.FromResult<object>(new { Rendering = "2D/3D", Widgets = true, Theming = true });
        }
    }
}
