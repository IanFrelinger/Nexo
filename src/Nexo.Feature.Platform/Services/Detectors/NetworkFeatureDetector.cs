using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Services.Detectors
{
    public class NetworkFeatureDetector
    {
        private readonly ILogger<NetworkFeatureDetector> _logger;

        public NetworkFeatureDetector(ILogger<NetworkFeatureDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PlatformFeature>> DetectNetworkFeaturesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting network features for {PlatformType}", platformType);

                var features = new List<PlatformFeature>();

                // Network connectivity
                features.AddRange(DetectConnectivityFeatures());

                // Network interfaces
                features.AddRange(DetectNetworkInterfaceFeatures());

                // Network protocols
                features.AddRange(DetectProtocolFeatures());

                return features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting network features");
                return new List<PlatformFeature>();
            }
        }

        public async Task<NetworkCapabilities> DetectNetworkCapabilitiesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting network capabilities for {PlatformType}", platformType);

                return new NetworkCapabilities
                {
                    IsConnected = IsNetworkConnected(),
                    InterfaceCount = GetNetworkInterfaceCount(),
                    SupportedProtocols = GetSupportedProtocols(),
                    BandwidthEstimate = GetBandwidthEstimate()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting network capabilities");
                return new NetworkCapabilities();
            }
        }

        private List<PlatformFeature> DetectConnectivityFeatures()
        {
            var features = new List<PlatformFeature>();

            // Network connectivity
            var isConnected = IsNetworkConnected();
            features.Add(new PlatformFeature
            {
                Name = "Network_Connectivity",
                Type = FeatureType.Network,
                Value = isConnected.ToString(),
                IsAvailable = isConnected,
                Description = "Network connectivity status"
            });

            // Internet connectivity
            var internetConnected = IsInternetConnected();
            features.Add(new PlatformFeature
            {
                Name = "Internet_Connectivity",
                Type = FeatureType.Network,
                Value = internetConnected.ToString(),
                IsAvailable = internetConnected,
                Description = "Internet connectivity status"
            });

            return features;
        }

        private List<PlatformFeature> DetectNetworkInterfaceFeatures()
        {
            var features = new List<PlatformFeature>();

            // Network interface count
            var interfaceCount = GetNetworkInterfaceCount();
            features.Add(new PlatformFeature
            {
                Name = "Network_Interfaces",
                Type = FeatureType.Network,
                Value = interfaceCount.ToString(),
                IsAvailable = interfaceCount > 0,
                Description = "Number of network interfaces"
            });

            // Active interfaces
            var activeInterfaces = GetActiveNetworkInterfaces();
            features.Add(new PlatformFeature
            {
                Name = "Active_Interfaces",
                Type = FeatureType.Network,
                Value = activeInterfaces.ToString(),
                IsAvailable = activeInterfaces > 0,
                Description = "Number of active network interfaces"
            });

            return features;
        }

        private List<PlatformFeature> DetectProtocolFeatures()
        {
            var features = new List<PlatformFeature>();

            // HTTP support
            features.Add(new PlatformFeature
            {
                Name = "HTTP_Support",
                Type = FeatureType.Network,
                Value = "true",
                IsAvailable = true,
                Description = "HTTP protocol support"
            });

            // HTTPS support
            features.Add(new PlatformFeature
            {
                Name = "HTTPS_Support",
                Type = FeatureType.Network,
                Value = "true",
                IsAvailable = true,
                Description = "HTTPS protocol support"
            });

            // WebSocket support
            features.Add(new PlatformFeature
            {
                Name = "WebSocket_Support",
                Type = FeatureType.Network,
                Value = "true",
                IsAvailable = true,
                Description = "WebSocket protocol support"
            });

            return features;
        }

        private bool IsNetworkConnected()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        private bool IsInternetConnected()
        {
            try
            {
                // Simple ping test to check internet connectivity
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private int GetNetworkInterfaceCount()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces().Length;
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveNetworkInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Count(ni => ni.OperationalStatus == OperationalStatus.Up);
            }
            catch
            {
                return 0;
            }
        }

        private List<string> GetSupportedProtocols()
        {
            return new List<string>
            {
                "HTTP",
                "HTTPS",
                "WebSocket",
                "TCP",
                "UDP"
            };
        }

        private long GetBandwidthEstimate()
        {
            try
            {
                // This is a simplified estimation
                // In a real implementation, you would perform actual bandwidth tests
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                var activeInterfaces = interfaces.Where(ni => ni.OperationalStatus == OperationalStatus.Up);
                
                if (activeInterfaces.Any())
                {
                    // Estimate based on interface type
                    var ethernetInterfaces = activeInterfaces.Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
                    if (ethernetInterfaces.Any())
                        return 1000 * 1000 * 1000; // 1 Gbps estimate for Ethernet
                    
                    var wifiInterfaces = activeInterfaces.Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
                    if (wifiInterfaces.Any())
                        return 100 * 1000 * 1000; // 100 Mbps estimate for WiFi
                }
                
                return 10 * 1000 * 1000; // 10 Mbps fallback estimate
            }
            catch
            {
                return 0;
            }
        }
    }
}
