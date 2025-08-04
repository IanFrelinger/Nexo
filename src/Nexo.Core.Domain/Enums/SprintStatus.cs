namespace Nexo.Core.Domain.Enums
{
    /// <summary>
    /// Represents the status of a sprint in the development lifecycle.
    /// </summary>
    public enum SprintStatus
    {
        /// <summary>
        /// Sprint is in the planning phase.
        /// </summary>
        Planning = 0,
        
        /// <summary>
        /// Sprint is active and in progress.
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// Sprint has been completed and closed.
        /// </summary>
        Closed = 2
    }
}