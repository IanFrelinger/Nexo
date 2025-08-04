namespace Nexo.Feature.AI.Enums
{
    /// <summary>
    /// Defines strategies for allocating AI resources.
    /// </summary>
    public enum ResourceAllocationStrategy
    {
        /// <summary>
        /// Balanced allocation across all resources.
        /// </summary>
        Balanced,
        
        /// <summary>
        /// Prioritize CPU usage.
        /// </summary>
        CpuOptimized,
        
        /// <summary>
        /// Prioritize memory usage.
        /// </summary>
        MemoryOptimized,
        
        /// <summary>
        /// Prioritize GPU usage.
        /// </summary>
        GpuOptimized,
        
        /// <summary>
        /// Prioritize network bandwidth.
        /// </summary>
        NetworkOptimized,
        
        /// <summary>
        /// Prioritize disk usage.
        /// </summary>
        DiskOptimized,
        
        /// <summary>
        /// Custom allocation based on specific requirements.
        /// </summary>
        Custom
    }
} 