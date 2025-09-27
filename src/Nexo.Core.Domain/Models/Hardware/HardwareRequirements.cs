using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Hardware
{
    /// <summary>
    /// Represents hardware requirements for Nexo
    /// </summary>
    public partial class HardwareRequirements
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Memory requirements
        public long MinimumMemoryBytes { get; set; }
        public long RecommendedMemoryBytes { get; set; }
        public long MaximumMemoryBytes { get; set; }
        
        // CPU requirements
        public int MinimumCpuCores { get; set; }
        public int RecommendedCpuCores { get; set; }
        public double MinimumCpuFrequencyGhz { get; set; }
        public double RecommendedCpuFrequencyGhz { get; set; }
        
        // Storage requirements
        public long MinimumStorageBytes { get; set; }
        public long RecommendedStorageBytes { get; set; }
        public long MaximumStorageBytes { get; set; }
        
        // GPU requirements (optional)
        public bool GpuRequired { get; set; }
        public string? GpuModel { get; set; }
        public long? GpuMemoryBytes { get; set; }
        
        // Network requirements
        public bool NetworkRequired { get; set; }
        public long MinimumBandwidthBps { get; set; }
        public long RecommendedBandwidthBps { get; set; }
        
        // Operating system requirements
        public List<string> SupportedOperatingSystems { get; set; } = new();
        public List<string> SupportedArchitectures { get; set; } = new();
        
        // Performance tiers
        public List<PerformanceTier> PerformanceTiers { get; set; } = new();
        
        // Cloud fallback options
        public List<CloudFallbackOption> CloudFallbackOptions { get; set; } = new();
    }

    /// <summary>
    /// Represents a performance tier
    /// </summary>
    public partial class PerformanceTier
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public PerformanceLevel Level { get; set; }
        public HardwareRequirements Requirements { get; set; } = new();
        public double EstimatedCostPerHour { get; set; }
        public List<string> Features { get; set; } = new();
        public bool IsRecommended { get; set; }
    }

    /// <summary>
    /// Represents a cloud fallback option
    /// </summary>
    public partial class CloudFallbackOption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public CloudProvider Provider { get; set; }
        public string Region { get; set; } = "";
        public HardwareRequirements Requirements { get; set; } = new();
        public PricingModel Pricing { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public bool IsAvailable { get; set; }
        public string? SetupInstructions { get; set; }
    }

    /// <summary>
    /// Represents pricing model for cloud services
    /// </summary>
    public partial class PricingModel
    {
        public double HourlyRate { get; set; }
        public double MonthlyRate { get; set; }
        public double AnnualRate { get; set; }
        public string Currency { get; set; } = "USD";
        public List<string> IncludedFeatures { get; set; } = new();
        public List<string> AdditionalCharges { get; set; } = new();
    }

    /// <summary>
    /// Represents current system capabilities
    /// </summary>
    public partial class SystemCapabilities
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        
        // Current system specs
        public long AvailableMemoryBytes { get; set; }
        public int CpuCores { get; set; }
        public double CpuFrequencyGhz { get; set; }
        public long AvailableStorageBytes { get; set; }
        public string OperatingSystem { get; set; } = "";
        public string Architecture { get; set; } = "";
        public bool HasGpu { get; set; }
        public string? GpuModel { get; set; }
        public long? GpuMemoryBytes { get; set; }
        public bool HasNetworkConnection { get; set; }
        public long BandwidthBps { get; set; }
        
        // Capability assessment
        public CapabilityLevel OverallCapability { get; set; }
        public List<CapabilityIssue> Issues { get; set; } = new();
        public List<CapabilityRecommendation> Recommendations { get; set; } = new();
        public bool CanRunNexo { get; set; }
        public bool CanRunWithCloudFallback { get; set; }
        public PerformanceTier? RecommendedTier { get; set; }
        public CloudFallbackOption? RecommendedCloudOption { get; set; }
    }

    /// <summary>
    /// Represents a capability issue
    /// </summary>
    public partial class CapabilityIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public IssueType Type { get; set; }
        public IssueSeverity Severity { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string RequiredValue { get; set; } = "";
        public string FixSuggestion { get; set; } = "";
    }

    /// <summary>
    /// Represents a capability recommendation
    /// </summary>
    public partial class CapabilityRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RecommendationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Implementation { get; set; } = "";
        public int Priority { get; set; }
        public double ImpactScore { get; set; }
        public double Cost { get; set; }
        public string? CostDescription { get; set; }
    }

    /// <summary>
    /// Performance levels
    /// </summary>
    public enum PerformanceLevel
    {
        Minimal = 1,
        Basic = 2,
        Standard = 3,
        High = 4,
        Premium = 5
    }

    /// <summary>
    /// Capability levels
    /// </summary>
    public enum CapabilityLevel
    {
        Insufficient = 1,
        Minimal = 2,
        Basic = 3,
        Good = 4,
        Excellent = 5
    }

    /// <summary>
    /// Cloud providers
    /// </summary>
    public enum CloudProvider
    {
        Azure,
        AWS,
        GoogleCloud,
        DigitalOcean,
        Linode,
        Vultr
    }

    /// <summary>
    /// Issue types
    /// </summary>
    public enum IssueType
    {
        Memory,
        Cpu,
        Storage,
        Gpu,
        Network,
        OperatingSystem,
        Architecture
    }

    /// <summary>
    /// Issue severity levels
    /// </summary>
    public enum IssueSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Recommendation types
    /// </summary>
    public enum RecommendationType
    {
        HardwareUpgrade,
        CloudMigration,
        ConfigurationOptimization,
        AlternativeSolution
    }
}
