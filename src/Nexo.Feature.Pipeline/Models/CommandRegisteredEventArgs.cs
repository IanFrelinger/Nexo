using System;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Event arguments for when a command is registered.
    /// </summary>
    public class CommandRegisteredEventArgs : EventArgs
    {
        /// <summary>
        /// The command that was registered.
        /// </summary>
        public ICommand Command { get; }
        
        /// <summary>
        /// Timestamp when the command was registered.
        /// </summary>
        public DateTime RegistrationTime { get; }
        
        public CommandRegisteredEventArgs(ICommand command)
        {
            Command = command;
            RegistrationTime = DateTime.UtcNow;
        }
    }
} 