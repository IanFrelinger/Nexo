using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    public class EnvironmentProfile
    {
        public string EnvironmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EnvironmentType { get; set; } = string.Empty;
        public PlatformType Platform { get; set; }
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        
        // Additional properties needed by the application
        public PlatformType PlatformType { get; set; }
        public int CpuCores { get; set; }
        public long AvailableMemoryMB { get; set; }
        public string Context { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public string FrameworkVersion { get; set; } = string.Empty;
        public bool HasChanged { get; set; }
    }
}