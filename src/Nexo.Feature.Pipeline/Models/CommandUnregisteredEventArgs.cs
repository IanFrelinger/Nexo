using System;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Event arguments for when a command is unregistered.
    /// </summary>
    public class CommandUnregisteredEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the command that was unregistered.
        /// </summary>
        public string CommandId { get; }
        
        /// <summary>
        /// Timestamp when the command was unregistered.
        /// </summary>
        public DateTime UnregistrationTime { get; }
        
        public CommandUnregisteredEventArgs(string commandId)
        {
            CommandId = commandId;
            UnregistrationTime = DateTime.UtcNow;
        }
    }
} 