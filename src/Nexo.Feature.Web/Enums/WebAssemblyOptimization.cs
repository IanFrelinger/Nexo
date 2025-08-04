namespace Nexo.Feature.Web.Enums
{
    /// <summary>
    /// WebAssembly optimization strategies for performance.
    /// </summary>
    public enum WebAssemblyOptimization
    {
        /// <summary>
        /// No optimization - development mode
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Basic optimization for better performance
        /// </summary>
        Basic = 1,
        
        /// <summary>
        /// Aggressive optimization for maximum performance
        /// </summary>
        Aggressive = 2,
        
        /// <summary>
        /// Size optimization for smaller bundle size
        /// </summary>
        Size = 3,
        
        /// <summary>
        /// Balanced optimization between performance and size
        /// </summary>
        Balanced = 4,
        
        /// <summary>
        /// Custom optimization with specific settings
        /// </summary>
        Custom = 5
    }
} 