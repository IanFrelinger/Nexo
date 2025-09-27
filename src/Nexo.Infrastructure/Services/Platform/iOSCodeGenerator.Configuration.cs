using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// iOS app configuration generation functionality
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        /// <summary>
        /// Generates app configuration files.
        /// </summary>
        public Task<iOSAppConfiguration> GenerateAppConfigurationAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = new iOSAppConfiguration
                {
                    AppName = applicationLogic.ApplicationName,
                    BundleIdentifier = $"com.{applicationLogic.ApplicationName.ToLower()}.app",
                    Version = "1.0.0",
                    BuildNumber = "1",
                    MinimumiOSVersion = "15.0",
                    TargetiOSVersion = "17.0",
                    SupportedOrientations = new[] { "Portrait", "Landscape" },
                    RequiredCapabilities = GetRequiredCapabilities(applicationLogic),
                    InfoPlistSettings = GenerateInfoPlistSettings(applicationLogic),
                    BuildSettings = GenerateBuildSettings(options)
                };

                return Task.FromResult(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating app configuration");
                return Task.FromResult(new iOSAppConfiguration());
            }
        }

        private string[] GetRequiredCapabilities(ApplicationLogic applicationLogic)
        {
            var capabilities = new List<string>();

            if (applicationLogic.Features.Any(f => f.RequiresCamera))
                capabilities.Add("camera");
            
            if (applicationLogic.Features.Any(f => f.RequiresLocation))
                capabilities.Add("location");
            
            if (applicationLogic.Features.Any(f => f.RequiresPushNotifications))
                capabilities.Add("push-notifications");
            
            if (applicationLogic.Features.Any(f => f.RequiresBluetooth))
                capabilities.Add("bluetooth");
            
            if (applicationLogic.Features.Any(f => f.RequiresMicrophone))
                capabilities.Add("microphone");

            return capabilities.ToArray();
        }

        private Dictionary<string, object> GenerateInfoPlistSettings(ApplicationLogic applicationLogic)
        {
            var settings = new Dictionary<string, object>
            {
                ["CFBundleDisplayName"] = applicationLogic.ApplicationName,
                ["CFBundleShortVersionString"] = "1.0.0",
                ["CFBundleVersion"] = "1",
                ["LSRequiresIPhoneOS"] = true,
                ["UILaunchStoryboardName"] = "LaunchScreen",
                ["UISupportedInterfaceOrientations"] = new[] { "UIInterfaceOrientationPortrait", "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight" }
            };

            // Add privacy usage descriptions based on features
            if (applicationLogic.Features.Any(f => f.RequiresCamera))
                settings["NSCameraUsageDescription"] = "This app needs access to camera to take photos.";
            
            if (applicationLogic.Features.Any(f => f.RequiresLocation))
                settings["NSLocationWhenInUseUsageDescription"] = "This app needs access to location to provide location-based services.";
            
            if (applicationLogic.Features.Any(f => f.RequiresMicrophone))
                settings["NSMicrophoneUsageDescription"] = "This app needs access to microphone to record audio.";

            return settings;
        }

        private Dictionary<string, string> GenerateBuildSettings(iOSGenerationOptions options)
        {
            return new Dictionary<string, string>
            {
                ["SWIFT_VERSION"] = "5.0",
                ["IPHONEOS_DEPLOYMENT_TARGET"] = "15.0",
                ["TARGETED_DEVICE_FAMILY"] = "1,2", // iPhone and iPad
                ["SUPPORTED_PLATFORMS"] = "iphoneos iphonesimulator",
                ["VALID_ARCHS"] = "arm64 arm64e",
                ["ONLY_ACTIVE_ARCH"] = "YES",
                ["ENABLE_BITCODE"] = "NO"
            };
        }
    }
}
