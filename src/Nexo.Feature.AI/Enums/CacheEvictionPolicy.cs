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
        LeastFrequentlyUsed,
        
        /// <summary>
        /// Remove items based on time since last access.
        /// </summary>
        TimeBased,
        
        /// <summary>
        /// Remove items based on size.
        /// </summary>
        SizeBased,
        
        /// <summary>
        /// Remove items randomly.
        /// </summary>
        Random,
        
        /// <summary>
        /// Remove items based on custom criteria.
        /// </summary>
        Custom
    }
} 