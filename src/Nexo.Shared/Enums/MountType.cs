namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents a bind mount, where a file or directory on the host is mounted into the container.
    /// </summary>
    public enum MountType
    {
        /// <summary>
        /// Represents a bind mount, where a file or directory on the host is mounted into the container.
        /// </summary>
        Bind = 0,

        /// <summary>
        /// Represents a volume-based mount type within the container configuration.
        /// </summary>
        Volume = 1,

        /// <summary>
        /// Represents a temporary filesystem mount, where data is stored in memory
        /// and does not persist beyond the lifecycle of the container.
        /// </summary>
        Tmpfs = 2
    }
}