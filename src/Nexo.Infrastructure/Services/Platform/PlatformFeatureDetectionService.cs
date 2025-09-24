using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Orchestrator for platform feature detection that delegates to specialized detectors.
    /// </summary>
    public class PlatformFeatureDetectionService : IPlatformFeatureDetectionService
    {
        private readonly ILogger<PlatformFeatureDetectionService> _logger;
        private readonly IUiFeatureDetector _uiDetector;
        private readonly IDataFeatureDetector _dataDetector;
        private readonly INetworkFeatureDetector _networkDetector;
        private readonly IHardwareFeatureDetector _hardwareDetector;
        private readonly ISecurityFeatureDetector _securityDetector;
        private readonly IPerformanceFeatureDetector _performanceDetector;

        public PlatformFeatureDetectionService(
            ILogger<PlatformFeatureDetectionService> logger,
            IUiFeatureDetector uiDetector,
            IDataFeatureDetector dataDetector,
            INetworkFeatureDetector networkDetector,
            IHardwareFeatureDetector hardwareDetector,
            ISecurityFeatureDetector securityDetector,
            IPerformanceFeatureDetector performanceDetector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiDetector = uiDetector ?? throw new ArgumentNullException(nameof(uiDetector));
            _dataDetector = dataDetector ?? throw new ArgumentNullException(nameof(dataDetector));
            _networkDetector = networkDetector ?? throw new ArgumentNullException(nameof(networkDetector));
            _hardwareDetector = hardwareDetector ?? throw new ArgumentNullException(nameof(hardwareDetector));
            _securityDetector = securityDetector ?? throw new ArgumentNullException(nameof(securityDetector));
            _performanceDetector = performanceDetector ?? throw new ArgumentNullException(nameof(performanceDetector));
        }

        public async Task<PlatformCapabilities> DetectCapabilitiesAsync(string platform, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Detecting platform capabilities for {Platform}", platform);

            var capabilities = new PlatformCapabilities
            {
                Platform = platform,
                Version = "1.0.0",
                SupportedFeatures = new List<string> { "UI", "Data", "Network", "Hardware", "Security", "Performance" },
                Metadata = new Dictionary<string, object>()
            };

            capabilities.Metadata["UICapabilities"] = await _uiDetector.DetectAsync(platform, cancellationToken);
            capabilities.Metadata["DataCapabilities"] = await _dataDetector.DetectAsync(platform, cancellationToken);
            capabilities.Metadata["NetworkCapabilities"] = await _networkDetector.DetectAsync(platform, cancellationToken);
            capabilities.Metadata["HardwareCapabilities"] = await _hardwareDetector.DetectAsync(platform, cancellationToken);
            capabilities.Metadata["SecurityCapabilities"] = await _securityDetector.DetectAsync(platform, cancellationToken);
            capabilities.Metadata["PerformanceCapabilities"] = await _performanceDetector.DetectAsync(platform, cancellationToken);

            capabilities.Metadata["DetectedAt"] = DateTimeOffset.UtcNow;
            capabilities.Metadata["Success"] = true;

            return capabilities;
        }
    }
}
