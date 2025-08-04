using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Request model for desktop code generation.
    /// </summary>
    public class DesktopCodeGenerationRequest
    {
        /// <summary>
        /// The target desktop platform (Windows, macOS, Linux).
        /// </summary>
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// The application type (Console, GUI, Service, etc.).
        /// </summary>
        public string ApplicationType { get; set; } = string.Empty;
        
        /// <summary>
        /// The UI framework to use (WPF, WinUI, Avalonia, etc.).
        /// </summary>
        public string UIFramework { get; set; } = string.Empty;
        
        /// <summary>
        /// The application name.
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;
        
        /// <summary>
        /// The application description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The application version.
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// The target .NET version.
        /// </summary>
        public string TargetFramework { get; set; } = "net8.0";
        
        /// <summary>
        /// Whether to include system integration features.
        /// </summary>
        public bool IncludeSystemIntegration { get; set; } = true;
        
        /// <summary>
        /// Whether to include native API bindings.
        /// </summary>
        public bool IncludeNativeAPIs { get; set; } = false;
        
        /// <summary>
        /// Whether to include performance optimizations.
        /// </summary>
        public bool IncludePerformanceOptimizations { get; set; } = true;
        
        /// <summary>
        /// The optimization level to apply.
        /// </summary>
        public DesktopOptimizationLevel OptimizationLevel { get; set; } = DesktopOptimizationLevel.Balanced;
        
        /// <summary>
        /// Additional features to include.
        /// </summary>
        public List<string> AdditionalFeatures { get; set; } = new List<string>();
        
        /// <summary>
        /// Custom configuration options.
        /// </summary>
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result model for desktop code generation.
    /// </summary>
    public class DesktopCodeGenerationResult
    {
        /// <summary>
        /// Whether the code generation was successful.
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The generated main application code.
        /// </summary>
        public string MainCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated UI code (if applicable).
        /// </summary>
        public string UICode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated configuration files.
        /// </summary>
        public Dictionary<string, string> ConfigurationFiles { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// The generated project files.
        /// </summary>
        public Dictionary<string, string> ProjectFiles { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// The generated system integration code.
        /// </summary>
        public string SystemIntegrationCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated native API bindings.
        /// </summary>
        public string NativeAPIBindings { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated deployment configuration.
        /// </summary>
        public DesktopDeploymentConfig DeploymentConfig { get; set; } = new DesktopDeploymentConfig();
        
        /// <summary>
        /// Performance analysis results.
        /// </summary>
        public DesktopPerformanceAnalysis PerformanceAnalysis { get; set; } = new DesktopPerformanceAnalysis();
        
        /// <summary>
        /// Any warnings or messages.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Any errors that occurred.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// The target platform.
        /// </summary>
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// The application type.
        /// </summary>
        public string ApplicationType { get; set; } = string.Empty;
        
        /// <summary>
        /// The UI framework used.
        /// </summary>
        public string UIFramework { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result model for desktop code validation.
    /// </summary>
    public class DesktopCodeValidationResult
    {
        /// <summary>
        /// Whether the code is valid.
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Any validation errors found.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Any validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Platform-specific compatibility issues.
        /// </summary>
        public List<string> CompatibilityIssues { get; set; } = new List<string>();
        
        /// <summary>
        /// Performance recommendations.
        /// </summary>
        public List<string> PerformanceRecommendations { get; set; } = new List<string>();
        
        /// <summary>
        /// Security recommendations.
        /// </summary>
        public List<string> SecurityRecommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration model for desktop deployment.
    /// </summary>
    public class DesktopDeploymentConfig
    {
        /// <summary>
        /// The deployment type (MSI, DMG, AppImage, etc.).
        /// </summary>
        public string DeploymentType { get; set; } = string.Empty;
        
        /// <summary>
        /// The installer configuration.
        /// </summary>
        public string InstallerConfig { get; set; } = string.Empty;
        
        /// <summary>
        /// The packaging configuration.
        /// </summary>
        public string PackagingConfig { get; set; } = string.Empty;
        
        /// <summary>
        /// The signing configuration.
        /// </summary>
        public string SigningConfig { get; set; } = string.Empty;
        
        /// <summary>
        /// The update configuration.
        /// </summary>
        public string UpdateConfig { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional deployment files.
        /// </summary>
        public Dictionary<string, string> AdditionalFiles { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Request model for desktop deployment configuration.
    /// </summary>
    public class DesktopDeploymentRequest
    {
        /// <summary>
        /// The deployment type.
        /// </summary>
        public string DeploymentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to include code signing.
        /// </summary>
        public bool IncludeCodeSigning { get; set; } = false;
        
        /// <summary>
        /// Whether to include auto-updates.
        /// </summary>
        public bool IncludeAutoUpdates { get; set; } = true;
        
        /// <summary>
        /// Whether to include crash reporting.
        /// </summary>
        public bool IncludeCrashReporting { get; set; } = true;
        
        /// <summary>
        /// The application icon path.
        /// </summary>
        public string IconPath { get; set; } = string.Empty;
        
        /// <summary>
        /// The application metadata.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Analysis model for desktop performance.
    /// </summary>
    public class DesktopPerformanceAnalysis
    {
        /// <summary>
        /// The estimated startup time in milliseconds.
        /// </summary>
        public int EstimatedStartupTime { get; set; }
        
        /// <summary>
        /// The estimated memory usage in MB.
        /// </summary>
        public int EstimatedMemoryUsage { get; set; }
        
        /// <summary>
        /// The estimated CPU usage percentage.
        /// </summary>
        public double EstimatedCpuUsage { get; set; }
        
        /// <summary>
        /// The estimated disk usage in MB.
        /// </summary>
        public int EstimatedDiskUsage { get; set; }
        
        /// <summary>
        /// Performance bottlenecks identified.
        /// </summary>
        public List<string> PerformanceBottlenecks { get; set; } = new List<string>();
        
        /// <summary>
        /// Optimization recommendations.
        /// </summary>
        public List<string> OptimizationRecommendations { get; set; } = new List<string>();
        
        /// <summary>
        /// Platform-specific performance notes.
        /// </summary>
        public Dictionary<string, string> PlatformNotes { get; set; } = new Dictionary<string, string>();
    }
} 