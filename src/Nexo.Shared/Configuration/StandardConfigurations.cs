using System;
using System.Collections.Generic;

namespace Nexo.Shared.Configuration
{
    /// <summary>
    /// Application-wide configuration
    /// </summary>
    public class ApplicationConfiguration : BaseConfiguration
    {
        public string Environment { get; set; } = "Development";
        public string LogLevel { get; set; } = "Information";
        public bool EnableMetrics { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetryAttempts { get; set; } = 3;
    }

    /// <summary>
    /// Database configuration
    /// </summary>
    public class DatabaseConfiguration : BaseConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Provider { get; set; } = "SqlServer";
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = true;
        public int MaxRetryCount { get; set; } = 3;
    }

    /// <summary>
    /// Agent configuration
    /// </summary>
    public class AgentConfiguration : BaseConfiguration
    {
        public int MaxConcurrentTasks { get; set; } = 10;
        public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableHealthChecks { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
        public List<string> AllowedCapabilities { get; set; } = new();
    }

    /// <summary>
    /// Security configuration
    /// </summary>
    public class SecurityConfiguration : BaseConfiguration
    {
        public bool RequireAuthentication { get; set; } = true;
        public string AuthenticationProvider { get; set; } = "JWT";
        public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromHours(1);
        public bool EnableEncryption { get; set; } = true;
        public string EncryptionKey { get; set; } = string.Empty;
    }
}
