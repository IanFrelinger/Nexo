using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Deployment
{
    /// <summary>
    /// Represents a deployment package for application deployment
    /// </summary>
    public class DeploymentPackage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public List<DeploymentFile> Files { get; set; } = new();
        public List<DeploymentDependency> Dependencies { get; set; } = new();
        public DeploymentConfiguration Configuration { get; set; } = new();
        public PackageType Type { get; set; } = PackageType.Application;
        public PackageStatus Status { get; set; } = PackageStatus.Created;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeployedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a file in the deployment package
    /// </summary>
    public class DeploymentFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public FileType Type { get; set; } = FileType.Code;
        public long Size { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a dependency in the deployment package
    /// </summary>
    public class DeploymentDependency
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DependencyType Type { get; set; } = DependencyType.NuGet;
        public bool IsRequired { get; set; } = true;
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents deployment configuration
    /// </summary>
    public class DeploymentConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();
        public List<ConfigurationFile> ConfigurationFiles { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an environment variable
    /// </summary>
    public class EnvironmentVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsSecret { get; set; }
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a configuration file
    /// </summary>
    public class ConfigurationFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ConfigurationFileType Type { get; set; } = ConfigurationFileType.Json;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of deployment packages
    /// </summary>
    public enum PackageType
    {
        Application,
        Service,
        Library,
        Database,
        Configuration,
        Resource
    }

    /// <summary>
    /// Status of deployment packages
    /// </summary>
    public enum PackageStatus
    {
        Created,
        Building,
        Built,
        Deploying,
        Deployed,
        Failed,
        RolledBack
    }

    /// <summary>
    /// Types of files in deployment packages
    /// </summary>
    public enum FileType
    {
        Code,
        Configuration,
        Resource,
        Documentation,
        Test,
        Script
    }

    /// <summary>
    /// Types of dependencies
    /// </summary>
    public enum DependencyType
    {
        NuGet,
        Npm,
        Maven,
        Gradle,
        CocoaPods,
        System
    }

    /// <summary>
    /// Types of configuration files
    /// </summary>
    public enum ConfigurationFileType
    {
        Json,
        Xml,
        Yaml,
        Ini,
        Properties,
        Environment
    }
}
