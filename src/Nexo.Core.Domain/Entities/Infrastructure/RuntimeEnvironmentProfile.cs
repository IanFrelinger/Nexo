using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    public class RuntimeEnvironmentProfile
    {
        public string Name { get; set; } = string.Empty;
        public PlatformType Platform { get; set; }
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Additional properties needed by the application
        public PlatformType CurrentPlatform { get; set; }
        public int CpuCores { get; set; }
        public long AvailableMemoryMB { get; set; }
        public bool IsConstrained { get; set; }
        public bool IsMobile { get; set; }
        public bool IsWeb { get; set; }
        public bool IsUnity { get; set; }
        public string FrameworkVersion { get; set; } = string.Empty;
        public string OptimizationLevel { get; set; } = string.Empty;
        public bool IsDebugMode { get; set; }
        public PlatformType PlatformType { get; set; }
    }
}
