namespace Nexo.Feature.Platform.Enums
{
    /// <summary>
    /// Types of performance tuning profiles.
    /// </summary>
    public enum TuningProfileType
    {
        /// <summary>
        /// Balanced performance profile
        /// </summary>
        Balanced,
        
        /// <summary>
        /// High performance profile
        /// </summary>
        HighPerformance,
        
        /// <summary>
        /// Power saving profile
        /// </summary>
        PowerSaving,
        
        /// <summary>
        /// Custom tuning profile
        /// </summary>
        Custom,
        
        /// <summary>
        /// Gaming profile
        /// </summary>
        Gaming,
        
        /// <summary>
        /// Development profile
        /// </summary>
        Development
    }

    /// <summary>
    /// Types of memory optimization.
    /// </summary>
    public enum MemoryOptimizationType
    {
        /// <summary>
        /// Garbage collection optimization
        /// </summary>
        GarbageCollection,
        
        /// <summary>
        /// Memory pooling
        /// </summary>
        MemoryPooling,
        
        /// <summary>
        /// Cache optimization
        /// </summary>
        CacheOptimization,
        
        /// <summary>
        /// Memory compression
        /// </summary>
        MemoryCompression,
        
        /// <summary>
        /// Memory defragmentation
        /// </summary>
        MemoryDefragmentation,
        
        /// <summary>
        /// Other memory optimization
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of battery optimization.
    /// </summary>
    public enum BatteryOptimizationType
    {
        /// <summary>
        /// CPU frequency scaling
        /// </summary>
        CPUFrequencyScaling,
        
        /// <summary>
        /// Display brightness optimization
        /// </summary>
        DisplayOptimization,
        
        /// <summary>
        /// Network power management
        /// </summary>
        NetworkPowerManagement,
        
        /// <summary>
        /// Background process optimization
        /// </summary>
        BackgroundProcessOptimization,
        
        /// <summary>
        /// Wake lock management
        /// </summary>
        WakeLockManagement,
        
        /// <summary>
        /// Other battery optimization
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of performance bottlenecks.
    /// </summary>
    public enum BottleneckType
    {
        /// <summary>
        /// CPU bottleneck
        /// </summary>
        CPU,
        
        /// <summary>
        /// Memory bottleneck
        /// </summary>
        Memory,
        
        /// <summary>
        /// Disk I/O bottleneck
        /// </summary>
        DiskIO,
        
        /// <summary>
        /// Network bottleneck
        /// </summary>
        Network,
        
        /// <summary>
        /// GPU bottleneck
        /// </summary>
        GPU,
        
        /// <summary>
        /// Battery bottleneck
        /// </summary>
        Battery,
        
        /// <summary>
        /// Other bottleneck
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of performance recommendations.
    /// </summary>
    public enum RecommendationType
    {
        /// <summary>
        /// Memory optimization recommendation
        /// </summary>
        MemoryOptimization,
        
        /// <summary>
        /// CPU optimization recommendation
        /// </summary>
        CPUOptimization,
        
        /// <summary>
        /// Battery optimization recommendation
        /// </summary>
        BatteryOptimization,
        
        /// <summary>
        /// Network optimization recommendation
        /// </summary>
        NetworkOptimization,
        
        /// <summary>
        /// Code optimization recommendation
        /// </summary>
        CodeOptimization,
        
        /// <summary>
        /// Configuration optimization recommendation
        /// </summary>
        ConfigurationOptimization,
        
        /// <summary>
        /// Other recommendation
        /// </summary>
        Other
    }
}
