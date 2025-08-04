namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents the initial state of a development session, where it is being created but not yet started.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// Indicates that the session is in the process of being created but has not yet started.
        /// </summary>
        Creating = 0,

        /// <summary>
        /// Indicates that the session is currently active and in progress.
        /// </summary>
        Running = 1,

        /// <summary>
        /// Indicates that the session is temporarily halted but can be resumed later.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Indicates that the session has been stopped and is no longer active or running.
        /// </summary>
        Stopped = 3,

        /// <summary>
        /// Indicates that the session has encountered an error or issue, resulting in a failed state.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Indicates that the session is in the process of being terminated. This state reflects that the session is closing or shutting down.
        /// </summary>
        Terminating = 5
    }
}