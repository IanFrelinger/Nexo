using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.Deployment;
using DeploymentTarget = Nexo.Core.Domain.Entities.FeatureFactory.DeploymentTarget;
using DeploymentPackage = Nexo.Core.Domain.Entities.FeatureFactory.DeploymentPackage;
using PackageType = Nexo.Core.Domain.Enums.FeatureFactory.PackageType;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Deployment
{
    /// <summary>
    /// Interface for managing application deployment
    /// </summary>
    public interface IDeploymentManager
    {
        /// <summary>
        /// Deploys an application to a specific target
        /// </summary>
        Task<DeploymentResult> DeployApplicationAsync(ApplicationLogicResult applicationLogic, DeploymentTarget target, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys an application to cloud providers
        /// </summary>
        Task<DeploymentResult> DeployToCloudAsync(ApplicationLogicResult applicationLogic, CloudProvider provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys an application using containers
        /// </summary>
        Task<DeploymentResult> DeployToContainerAsync(ApplicationLogicResult applicationLogic, ContainerPlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys an application to desktop platforms
        /// </summary>
        Task<DeploymentResult> DeployToDesktopAsync(ApplicationLogicResult applicationLogic, DesktopPlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys an application to mobile platforms
        /// </summary>
        Task<DeploymentResult> DeployToMobileAsync(ApplicationLogicResult applicationLogic, MobilePlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deploys an application to web platforms
        /// </summary>
        Task<DeploymentResult> DeployToWebAsync(ApplicationLogicResult applicationLogic, WebPlatform platform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a deployment package from application logic
        /// </summary>
        Task<DeploymentPackage> CreateDeploymentPackageAsync(ApplicationLogicResult applicationLogic, PackageType packageType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a deployment target
        /// </summary>
        Task<ValidationResult> ValidateDeploymentTargetAsync(DeploymentTarget target, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets deployment status
        /// </summary>
        Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back a deployment
        /// </summary>
        Task<RollbackResult> RollbackDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of deployment operations
    /// </summary>
    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string DeploymentId { get; set; } = string.Empty;
        public DeploymentTarget Target { get; set; } = new();
        public DeploymentPackage Package { get; set; } = new();
        public DeploymentStatus Status { get; set; } = new();
        public List<DeploymentLog> Logs { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of rollback operations
    /// </summary>
    public class RollbackResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string RollbackId { get; set; } = string.Empty;
        public DeploymentStatus Status { get; set; } = new();
        public DateTime RolledBackAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Deployment status information
    /// </summary>
    public class DeploymentStatus
    {
        public string Id { get; set; } = string.Empty;
        public DeploymentState State { get; set; } = DeploymentState.Pending;
        public string Message { get; set; } = string.Empty;
        public int Progress { get; set; }
        public List<DeploymentStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Deployment log entry
    /// </summary>
    public class DeploymentLog
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel Level { get; set; } = LogLevel.Information;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Deployment step information
    /// </summary>
    public class DeploymentStep
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Validation error information
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Validation warning information
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Enums for deployment operations

    /// <summary>
    /// Cloud providers for deployment
    /// </summary>
    public enum CloudProvider
    {
        Azure,
        AWS,
        GCP,
        IBM,
        Oracle,
        Alibaba
    }

    /// <summary>
    /// Container platforms for deployment
    /// </summary>
    public enum ContainerPlatform
    {
        Docker,
        Kubernetes,
        OpenShift,
        AzureContainerInstances,
        AWSEKS,
        GCPGKE
    }

    /// <summary>
    /// Desktop platforms for deployment
    /// </summary>
    public enum DesktopPlatform
    {
        Windows,
        macOS,
        Linux,
        CrossPlatform
    }

    /// <summary>
    /// Mobile platforms for deployment
    /// </summary>
    public enum MobilePlatform
    {
        iOS,
        Android,
        CrossPlatform
    }

    /// <summary>
    /// Web platforms for deployment
    /// </summary>
    public enum WebPlatform
    {
        AzureAppService,
        AWSElasticBeanstalk,
        GCPAppEngine,
        Heroku,
        Vercel,
        Netlify
    }

    /// <summary>
    /// Deployment states
    /// </summary>
    public enum DeploymentState
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        RolledBack
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Step status
    /// </summary>
    public enum StepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
