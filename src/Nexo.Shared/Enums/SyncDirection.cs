namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents file synchronization from the container to the host environment.
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>
        /// Represents a synchronization direction where file changes are synchronized
        /// from the host environment to the container, ensuring the container's data
        /// is updated with the latest changes from the host.
        /// </summary>
        HostToContainer = 0,

        /// <summary>
        /// Represents a synchronization direction where file changes are synchronized in both directions
        /// between the host and the container, ensuring alignment of data on both sides.
        /// </summary>
        ContainerToHost = 1,

        /// <summary>
        /// Represents a synchronization direction where file changes are synchronized
        /// in both directions between the host environment and the container, ensuring
        /// that changes made in either environment are reflected in the other.
        /// </summary>
        Bidirectional = 2
    }
}