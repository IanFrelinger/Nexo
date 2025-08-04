using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Defines resource requirements for aggregators.
    /// </summary>
    public class ResourceRequirements
    {
        /// <summary>
        /// Minimum memory requirement in bytes.
        /// </summary>
        public ulong MinMemoryBytes { get; set; }
        
        /// <summary>
        /// Maximum memory requirement in bytes.
        /// </summary>
        public ulong MaxMemoryBytes { get; set; }
        
        /// <summary>
        /// Minimum CPU requirement (number of cores).
        /// </summary>
        public double MinCpuCores { get; set; }
        
        /// <summary>
        /// Maximum CPU requirement (number of cores).
        /// </summary>
        public double MaxCpuCores { get; set; }
        
        /// <summary>
        /// Minimum disk space requirement in bytes.
        /// </summary>
        public ulong MinDiskSpaceBytes { get; set; }
        
        /// <summary>
        /// Maximum disk space requirement in bytes.
        /// </summary>
        public ulong MaxDiskSpaceBytes { get; set; }
        
        /// <summary>
        /// Whether GPU is required.
        /// </summary>
        public bool RequiresGpu { get; set; }
        
        /// <summary>
        /// Minimum GPU memory requirement in bytes.
        /// </summary>
        public ulong MinGpuMemoryBytes { get; set; }
        
        /// <summary>
        /// Maximum GPU memory requirement in bytes.
        /// </summary>
        public ulong MaxGpuMemoryBytes { get; set; }
        
        /// <summary>
        /// Network bandwidth requirement in bytes per second.
        /// </summary>
        public ulong NetworkBandwidthBytesPerSecond { get; set; }
        
        /// <summary>
        /// Maximum execution time in milliseconds.
        /// </summary>
        public long MaxExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Whether the aggregator requires elevated privileges.
        /// </summary>
        public bool RequiresElevatedPrivileges { get; set; }
        
        /// <summary>
        /// Required operating system platforms.
        /// </summary>
        public List<string> RequiredPlatforms { get; set; } = new List<string>();
        
        /// <summary>
        /// Required software dependencies.
        /// </summary>
        public List<string> RequiredSoftware { get; set; } = new List<string>();
        
        /// <summary>
        /// Required hardware capabilities.
        /// </summary>
        public List<string> RequiredHardware { get; set; } = new List<string>();
        
        /// <summary>
        /// Additional resource requirements.
        /// </summary>
        public Dictionary<string, object> AdditionalRequirements { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates minimal resource requirements.
        /// </summary>
        /// <returns>Minimal resource requirements.</returns>
        public static ResourceRequirements Minimal()
        {
            return new ResourceRequirements
            {
                MinMemoryBytes = 64UL * 1024 * 1024, // 64 MB
                MaxMemoryBytes = 512UL * 1024 * 1024, // 512 MB
                MinCpuCores = 0.1,
                MaxCpuCores = 1.0,
                MinDiskSpaceBytes = 10UL * 1024 * 1024, // 10 MB
                MaxDiskSpaceBytes = 100UL * 1024 * 1024, // 100 MB
                MaxExecutionTimeMs = 30000 // 30 seconds
            };
        }
        
        /// <summary>
        /// Creates standard resource requirements.
        /// </summary>
        /// <returns>Standard resource requirements.</returns>
        public static ResourceRequirements Standard()
        {
            return new ResourceRequirements
            {
                MinMemoryBytes = 256UL * 1024 * 1024, // 256 MB
                MaxMemoryBytes = 2UL * 1024 * 1024 * 1024, // 2 GB
                MinCpuCores = 0.5,
                MaxCpuCores = 2.0,
                MinDiskSpaceBytes = 100UL * 1024 * 1024, // 100 MB
                MaxDiskSpaceBytes = 1UL * 1024 * 1024 * 1024, // 1 GB
                MaxExecutionTimeMs = 300000 // 5 minutes
            };
        }
        
        /// <summary>
        /// Creates high-performance resource requirements.
        /// </summary>
        /// <returns>High-performance resource requirements.</returns>
        public static ResourceRequirements HighPerformance()
        {
            return new ResourceRequirements
            {
                MinMemoryBytes = 2UL * 1024 * 1024 * 1024, // 2 GB
                MaxMemoryBytes = 16UL * 1024 * 1024 * 1024, // 16 GB
                MinCpuCores = 2.0,
                MaxCpuCores = 8.0,
                MinDiskSpaceBytes = 1UL * 1024 * 1024 * 1024, // 1 GB
                MaxDiskSpaceBytes = 10UL * 1024 * 1024 * 1024, // 10 GB
                MaxExecutionTimeMs = 1800000 // 30 minutes
            };
        }
        
        /// <summary>
        /// Creates AI-heavy resource requirements.
        /// </summary>
        /// <returns>AI-heavy resource requirements.</returns>
        public static ResourceRequirements AIHeavy()
        {
            return new ResourceRequirements
            {
                MinMemoryBytes = 8UL * 1024 * 1024 * 1024, // 8 GB
                MaxMemoryBytes = 64UL * 1024 * 1024 * 1024, // 64 GB
                MinCpuCores = 4.0,
                MaxCpuCores = 16.0,
                MinDiskSpaceBytes = 5UL * 1024 * 1024 * 1024, // 5 GB
                MaxDiskSpaceBytes = 50UL * 1024 * 1024 * 1024, // 50 GB
                RequiresGpu = true,
                MinGpuMemoryBytes = 4UL * 1024 * 1024 * 1024, // 4 GB
                MaxGpuMemoryBytes = 24UL * 1024 * 1024 * 1024, // 24 GB
                MaxExecutionTimeMs = 3600000 // 1 hour
            };
        }
        
        /// <summary>
        /// Adds a required platform.
        /// </summary>
        /// <param name="platform">The platform to add.</param>
        /// <returns>This resource requirements for chaining.</returns>
        public ResourceRequirements AddRequiredPlatform(string platform)
        {
            RequiredPlatforms.Add(platform);
            return this;
        }
        
        /// <summary>
        /// Adds required software.
        /// </summary>
        /// <param name="software">The software to add.</param>
        /// <returns>This resource requirements for chaining.</returns>
        public ResourceRequirements AddRequiredSoftware(string software)
        {
            RequiredSoftware.Add(software);
            return this;
        }
        
        /// <summary>
        /// Adds required hardware.
        /// </summary>
        /// <param name="hardware">The hardware to add.</param>
        /// <returns>This resource requirements for chaining.</returns>
        public ResourceRequirements AddRequiredHardware(string hardware)
        {
            RequiredHardware.Add(hardware);
            return this;
        }
        
        /// <summary>
        /// Adds additional requirements.
        /// </summary>
        /// <param name="key">The requirement key.</param>
        /// <param name="value">The requirement value.</param>
        /// <returns>This resource requirements for chaining.</returns>
        public ResourceRequirements AddAdditionalRequirement(string key, object value)
        {
            AdditionalRequirements[key] = value;
            return this;
        }
        
        /// <summary>
        /// Creates a new resource requirements instance with default values.
        /// </summary>
        /// <returns>A new resource requirements instance.</returns>
        public static ResourceRequirements CreateDefault()
        {
            return Standard();
        }
        
        /// <summary>
        /// Creates a resource requirements instance for AI workloads.
        /// </summary>
        /// <returns>A resource requirements instance for AI workloads.</returns>
        public static ResourceRequirements CreateAIWorkload()
        {
            return AIHeavy();
        }
        
        /// <summary>
        /// Adds a custom resource requirement.
        /// </summary>
        /// <param name="key">The requirement key.</param>
        /// <param name="value">The requirement value.</param>
        /// <returns>This resource requirements instance for method chaining.</returns>
        public ResourceRequirements AddCustomRequirement(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                AdditionalRequirements[key] = value;
            }
            return this;
        }
    }
} 