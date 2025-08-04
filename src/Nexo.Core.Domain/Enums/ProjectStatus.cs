namespace Nexo.Core.Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a project.
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>
        /// Project has been created but not initialized.
        /// </summary>
        NotInitialized = 0,
        
        /// <summary>
        /// Project has been initialized and is ready to run.
        /// </summary>
        Initialized = 1,
        
        /// <summary>
        /// Project is currently running.
        /// </summary>
        Running = 2,
        
        /// <summary>
        /// Project has been stopped.
        /// </summary>
        Stopped = 3,
        
        /// <summary>
        /// Project has failed.
        /// </summary>
        Failed = 4
    }
}