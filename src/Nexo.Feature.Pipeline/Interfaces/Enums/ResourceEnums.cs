namespace Nexo.Feature.Pipeline.Interfaces.Enums
{
    /// <summary>
    /// Resource priority levels.
    /// </summary>
    public enum ResourcePriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Resource types.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// CPU resource.
        /// </summary>
        Cpu,

        /// <summary>
        /// Memory resource.
        /// </summary>
        Memory,

        /// <summary>
        /// Disk resource.
        /// </summary>
        Disk,

        /// <summary>
        /// Network resource.
        /// </summary>
        Network
    }
}
