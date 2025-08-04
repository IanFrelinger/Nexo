namespace Nexo.Feature.AI.Enums
{
    /// <summary>
    /// Defines strategies for selecting AI models.
    /// </summary>
    public enum ModelSelectionStrategy
    {
        /// <summary>
        /// Always use the primary model.
        /// </summary>
        Primary,
        
        /// <summary>
        /// Use the model with the lowest cost.
        /// </summary>
        CostOptimized,
        
        /// <summary>
        /// Use the model with the best performance.
        /// </summary>
        PerformanceOptimized,
        
        /// <summary>
        /// Use the model with the best quality output.
        /// </summary>
        QualityOptimized,
        
        /// <summary>
        /// Automatically select based on request characteristics.
        /// </summary>
        Adaptive,
        
        /// <summary>
        /// Round-robin between available models.
        /// </summary>
        RoundRobin,
        
        /// <summary>
        /// Use the model with the lowest latency.
        /// </summary>
        LatencyOptimized
    }
} 