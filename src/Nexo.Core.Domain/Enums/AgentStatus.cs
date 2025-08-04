namespace Nexo.Core.Domain.Enums
{
    /// <summary>
    /// Represents the current operational status of an agent.
    /// </summary>
    public enum AgentStatus
    {
        /// <summary>
        /// Agent is not active.
        /// </summary>
        Inactive = 0,
        
        /// <summary>
        /// Agent is active and available for work.
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// Agent is currently busy with a task.
        /// </summary>
        Busy = 2,
        
        /// <summary>
        /// Agent has encountered a failure.
        /// </summary>
        Failed = 3
    }
}