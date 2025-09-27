using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Services.Detectors
{
    public partial class SoftwareFeatureDetector
    {
        private readonly ILogger<SoftwareFeatureDetector> _logger;

        public SoftwareFeatureDetector(ILogger<SoftwareFeatureDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PlatformFeature>> DetectSoftwareFeaturesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting software features for {PlatformType}", platformType);

                var features = new List<PlatformFeature>();

                // Operating system features
                features.AddRange(DetectOperatingSystemFeatures());

                // Runtime features
                features.AddRange(DetectRuntimeFeatures());

                // Development tools features
                features.AddRange(DetectDevelopmentToolsFeatures());

                // Database features
                features.AddRange(DetectDatabaseFeatures());

                return features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting software features");
                return new List<PlatformFeature>();
            }
        }

        public async Task<SoftwareCapabilities> DetectSoftwareCapabilitiesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting software capabilities for {PlatformType}", platformType);

                return new SoftwareCapabilities
                {
                    OperatingSystem = GetOperatingSystemInfo(),
                    RuntimeVersion = GetRuntimeVersion(),
                    DevelopmentTools = GetAvailableDevelopmentTools(),
                    DatabaseSupport = GetDatabaseSupport()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting software capabilities");
                return new SoftwareCapabilities();
            }
        }

        private List<PlatformFeature> DetectOperatingSystemFeatures()
        {
            var features = new List<PlatformFeature>();

            // OS version
            features.Add(new PlatformFeature
            {
                Name = "OS_Version",
                Type = FeatureType.Software,
                Value = Environment.OSVersion.VersionString,
                IsAvailable = true,
                Description = "Operating system version"
            });

            // OS platform
            features.Add(new PlatformFeature
            {
                Name = "OS_Platform",
                Type = FeatureType.Software,
                Value = RuntimeInformation.OSDescription,
                IsAvailable = true,
                Description = "Operating system platform"
            });

            // Architecture
            features.Add(new PlatformFeature
            {
                Name = "OS_Architecture",
                Type = FeatureType.Software,
                Value = RuntimeInformation.OSArchitecture.ToString(),
                IsAvailable = true,
                Description = "Operating system architecture"
            });

            return features;
        }

        private List<PlatformFeature> DetectRuntimeFeatures()
        {
            var features = new List<PlatformFeature>();

            // .NET version
            features.Add(new PlatformFeature
            {
                Name = "NET_Version",
                Type = FeatureType.Software,
                Value = Environment.Version.ToString(),
                IsAvailable = true,
                Description = ".NET runtime version"
            });

            // Framework description
            features.Add(new PlatformFeature
            {
                Name = "Framework_Description",
                Type = FeatureType.Software,
                Value = RuntimeInformation.FrameworkDescription,
                IsAvailable = true,
                Description = "Framework description"
            });

            return features;
        }

        private List<PlatformFeature> DetectDevelopmentToolsFeatures()
        {
            var features = new List<PlatformFeature>();

            // Git support
            var gitSupport = DetectGitSupport();
            features.Add(new PlatformFeature
            {
                Name = "Git_Support",
                Type = FeatureType.Software,
                Value = gitSupport.ToString(),
                IsAvailable = gitSupport,
                Description = "Git version control support"
            });

            // Docker support
            var dockerSupport = DetectDockerSupport();
            features.Add(new PlatformFeature
            {
                Name = "Docker_Support",
                Type = FeatureType.Software,
                Value = dockerSupport.ToString(),
                IsAvailable = dockerSupport,
                Description = "Docker containerization support"
            });

            return features;
        }

        private List<PlatformFeature> DetectDatabaseFeatures()
        {
            var features = new List<PlatformFeature>();

            // SQLite support
            features.Add(new PlatformFeature
            {
                Name = "SQLite_Support",
                Type = FeatureType.Software,
                Value = "true",
                IsAvailable = true,
                Description = "SQLite database support"
            });

            return features;
        }

        private bool DetectGitSupport()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool DetectDockerSupport()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetOperatingSystemInfo()
        {
            return $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        }

        private string GetRuntimeVersion()
        {
            return RuntimeInformation.FrameworkDescription;
        }

        private List<string> GetAvailableDevelopmentTools()
        {
            var tools = new List<string>();

            if (DetectGitSupport())
                tools.Add("Git");

            if (DetectDockerSupport())
                tools.Add("Docker");

            return tools;
        }

        private List<string> GetDatabaseSupport()
        {
            return new List<string> { "SQLite" };
        }
    }
}
