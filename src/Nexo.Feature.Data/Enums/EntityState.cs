namespace Nexo.Feature.Data.Enums
{
    /// <summary>
    /// Entity states for change tracking
    /// </summary>
    public enum EntityState
    {
        /// <summary>
        /// Entity is detached from the context
        /// </summary>
        Detached,

        /// <summary>
        /// Entity is unchanged
        /// </summary>
        Unchanged,

        /// <summary>
        /// Entity has been added
        /// </summary>
        Added,

        /// <summary>
        /// Entity has been deleted
        /// </summary>
        Deleted,

        /// <summary>
        /// Entity has been modified
        /// </summary>
        Modified
    }
} 