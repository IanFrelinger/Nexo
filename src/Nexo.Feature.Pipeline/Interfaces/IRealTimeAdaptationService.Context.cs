using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Environment context and related models for real-time adaptation
    /// </summary>
    public partial interface IRealTimeAdaptationService
    {
        // Context models are defined in separate files
    }

    /// <summary>
    /// Environment context for adaptation.
    /// </summary>
    public partial class EnvironmentContext
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        public string EnvironmentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the environment properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the performance requirements for this environment.
        /// </summary>
        public PerformanceRequirements PerformanceRequirements { get; set; } = new PerformanceRequirements();

        /// <summary>
        /// Gets or sets the resource constraints for this environment.
        /// </summary>
        public ResourceConstraints ResourceConstraints { get; set; } = new ResourceConstraints();

        /// <summary>
        /// Gets or sets the timestamp when this context was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Environment types for adaptation.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// Development environment.
        /// </summary>
        Development,

        /// <summary>
        /// Testing environment.
        /// </summary>
        Testing,

        /// <summary>
        /// Staging environment.
        /// </summary>
        Staging,

        /// <summary>
        /// Production environment.
        /// </summary>
        Production,

        /// <summary>
        /// Custom environment.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Resource constraints for environment adaptation.
    /// </summary>
    public partial class ResourceConstraints
    {
        /// <summary>
        /// Gets or sets the maximum CPU usage percentage.
        /// </summary>
        public double MaxCpuUsagePercentage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the maximum memory usage in MB.
        /// </summary>
        public long MaxMemoryUsageMB { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the maximum execution time in milliseconds.
        /// </summary>
        public long MaxExecutionTimeMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the maximum concurrent operations.
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether aggressive optimization is allowed.
        /// </summary>
        public bool AllowAggressiveOptimization { get; set; } = false;

        /// <summary>
        /// Gets or sets whether experimental features are enabled.
        /// </summary>
        public bool EnableExperimentalFeatures { get; set; } = false;
    }
}
