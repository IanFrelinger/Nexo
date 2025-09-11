using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Deployment
{
    /// <summary>
    /// Represents a deployment target for application deployment
    /// </summary>
    public class DeploymentTarget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TargetType Type { get; set; } = TargetType.Cloud;
        public TargetPlatform Platform { get; set; } = TargetPlatform.Azure;
        public string Environment { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public List<TargetResource> Resources { get; set; } = new();
        public TargetConfiguration Configuration { get; set; } = new();
        public TargetCredentials Credentials { get; set; } = new();
        public TargetStatus Status { get; set; } = TargetStatus.Available;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastDeployedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a resource in the deployment target
    /// </summary>
    public class TargetResource
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ResourceStatus Status { get; set; } = ResourceStatus.Available;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents target configuration
    /// </summary>
    public class TargetConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public List<NetworkConfiguration> NetworkSettings { get; set; } = new();
        public List<SecurityConfiguration> SecuritySettings { get; set; } = new();
        public List<ScalingConfiguration> ScalingSettings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents target credentials
    /// </summary>
    public class TargetCredentials
    {
        public string Provider { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public bool IsEncrypted { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents network configuration
    /// </summary>
    public class NetworkConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents security configuration
    /// </summary>
    public class SecurityConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents scaling configuration
    /// </summary>
    public class ScalingConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of deployment targets
    /// </summary>
    public enum TargetType
    {
        Cloud,
        OnPremises,
        Hybrid,
        Edge,
        Mobile,
        Desktop
    }

    /// <summary>
    /// Target platforms for deployment
    /// </summary>
    public enum TargetPlatform
    {
        Azure,
        AWS,
        GCP,
        Docker,
        Kubernetes,
        Windows,
        Linux,
        macOS,
        iOS,
        Android,
        Web
    }

    /// <summary>
    /// Status of deployment targets
    /// </summary>
    public enum TargetStatus
    {
        Available,
        Busy,
        Maintenance,
        Unavailable,
        Error
    }

    /// <summary>
    /// Status of target resources
    /// </summary>
    public enum ResourceStatus
    {
        Available,
        Busy,
        Maintenance,
        Unavailable,
        Error
    }
}
