using System;
using System.Collections.Generic;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// iOS app configuration.
    /// </summary>
    public partial class IOSAppConfiguration
    {
        public string AppName { get; set; } = string.Empty;
        public string BundleIdentifier { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public IOSDeploymentTarget DeploymentTarget { get; set; } = new IOSDeploymentTarget();
        public List<IOSCapability> Capabilities { get; set; } = new List<IOSCapability>();
        public List<IOSPermission> Permissions { get; set; } = new List<IOSPermission>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS deployment target configuration.
    /// </summary>
    public partial class IOSDeploymentTarget
    {
        public string MinimumVersion { get; set; } = "15.0";
        public string TargetVersion { get; set; } = "17.0";
        public List<string> SupportedDevices { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS capability configuration.
    /// </summary>
    public partial class IOSCapability
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS permission configuration.
    /// </summary>
    public partial class IOSPermission
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UsageDescription { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
