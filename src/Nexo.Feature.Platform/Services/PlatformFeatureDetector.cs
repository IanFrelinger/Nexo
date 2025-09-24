using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using Nexo.Feature.Platform.Services.Detectors;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// Orchestrator for platform feature detection that delegates to specialized detectors.
    /// </summary>
    public class PlatformFeatureDetector : IPlatformFeatureDetector
    {
        private readonly ILogger<PlatformFeatureDetector> _logger;
        private readonly Dictionary<string, PlatformFeature> _featureCache;
        private readonly Dictionary<PlatformType, PlatformCapabilitiesResult> _capabilitiesCache;
        private readonly object _cacheLock = new object();
        private readonly HardwareFeatureDetector _hardwareDetector;
        private readonly SoftwareFeatureDetector _softwareDetector;
        private readonly NetworkFeatureDetector _networkDetector;
        private readonly SecurityFeatureDetector _securityDetector;
        private readonly PerformanceFeatureDetector _performanceDetector;

        public PlatformFeatureDetector(ILogger<PlatformFeatureDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _featureCache = new Dictionary<string, PlatformFeature>();
            _capabilitiesCache = new Dictionary<PlatformType, PlatformCapabilitiesResult>();
            _hardwareDetector = new HardwareFeatureDetector(logger);
            _softwareDetector = new SoftwareFeatureDetector(logger);
            _networkDetector = new NetworkFeatureDetector(logger);
            _securityDetector = new SecurityFeatureDetector(logger);
            _performanceDetector = new PerformanceFeatureDetector(logger);
        }

        public async Task<PlatformFeatureDetectionResult> DetectPlatformFeaturesAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting platform feature detection for current platform");

            try
            {
                var currentPlatform = GetCurrentPlatform();
                var result = await DetectFeaturesForPlatformAsync(currentPlatform, cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("Platform feature detection completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during platform feature detection");
                return new PlatformFeatureDetectionResult
                {
                    IsSuccess = false,
                    Message = $"Error during platform feature detection: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<PlatformCapabilitiesResult> DetectCapabilitiesAsync(PlatformType platformType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Detecting capabilities for platform: {PlatformType}", platformType);

                lock (_cacheLock)
                {
                    if (_capabilitiesCache.ContainsKey(platformType))
                    {
                        _logger.LogInformation("Returning cached capabilities for {PlatformType}", platformType);
                        return _capabilitiesCache[platformType];
                    }
                }

                var capabilities = new PlatformCapabilitiesResult
                {
                    PlatformType = platformType,
                    HardwareCapabilities = await _hardwareDetector.DetectHardwareCapabilitiesAsync(platformType, cancellationToken),
                    SoftwareCapabilities = await _softwareDetector.DetectSoftwareCapabilitiesAsync(platformType, cancellationToken),
                    NetworkCapabilities = await _networkDetector.DetectNetworkCapabilitiesAsync(platformType, cancellationToken),
                    SecurityCapabilities = await _securityDetector.DetectSecurityCapabilitiesAsync(platformType, cancellationToken),
                    PerformanceCapabilities = await _performanceDetector.DetectPerformanceCapabilitiesAsync(platformType, cancellationToken)
                };

                lock (_cacheLock)
                {
                    _capabilitiesCache[platformType] = capabilities;
                }

                return capabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting capabilities for platform {PlatformType}", platformType);
                return new PlatformCapabilitiesResult
                {
                    PlatformType = platformType,
                    IsSuccess = false,
                    Message = $"Error detecting capabilities: {ex.Message}"
                };
            }
        }

        private async Task<PlatformFeatureDetectionResult> DetectFeaturesForPlatformAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                var features = new List<PlatformFeature>();

                // Detect hardware features
                var hardwareFeatures = await _hardwareDetector.DetectHardwareFeaturesAsync(platformType, cancellationToken);
                features.AddRange(hardwareFeatures);

                // Detect software features
                var softwareFeatures = await _softwareDetector.DetectSoftwareFeaturesAsync(platformType, cancellationToken);
                features.AddRange(softwareFeatures);

                // Detect network features
                var networkFeatures = await _networkDetector.DetectNetworkFeaturesAsync(platformType, cancellationToken);
                features.AddRange(networkFeatures);

                // Detect security features
                var securityFeatures = await _securityDetector.DetectSecurityFeaturesAsync(platformType, cancellationToken);
                features.AddRange(securityFeatures);

                // Detect performance features
                var performanceFeatures = await _performanceDetector.DetectPerformanceFeaturesAsync(platformType, cancellationToken);
                features.AddRange(performanceFeatures);

                return new PlatformFeatureDetectionResult
                {
                    IsSuccess = true,
                    Message = $"Successfully detected {features.Count} platform features",
                    PlatformType = platformType,
                    Features = features
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting features for platform {PlatformType}", platformType);
                return new PlatformFeatureDetectionResult
                {
                    IsSuccess = false,
                    Message = $"Error detecting features: {ex.Message}",
                    PlatformType = platformType,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private PlatformType GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return PlatformType.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return PlatformType.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return PlatformType.macOS;
            else
            return PlatformType.Unknown;
        }
    }
} 
