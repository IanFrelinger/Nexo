namespace Nexo.Feature.AI.Enums
{
    /// <summary>
    /// Defines cache eviction policies.
    /// </summary>
    public enum CacheEvictionPolicy
    {
        /// <summary>
        /// Remove least recently used items first.
        /// </summary>
        LeastRecentlyUsed,
        
        /// <summary>
        /// Remove least frequently used items first.
        /// </summary>
        LeastFrequentlyUsed
    }
} 