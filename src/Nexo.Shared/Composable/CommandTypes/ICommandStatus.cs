using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command status
    /// </summary>
    public interface ICommandStatus
    {
        string State { get; }
        string Message { get; }
        DateTime LastUpdated { get; }
        bool IsExecutable { get; }
        bool IsCompleted { get; }
        bool IsFailed { get; }
    }
    
    /// <summary>
    /// Default implementation of command status
    /// </summary>
    public class CommandStatus : ICommandStatus
    {
        public string State { get; }
        public string Message { get; }
        public DateTime LastUpdated { get; }
        public bool IsExecutable { get; }
        public bool IsCompleted { get; }
        public bool IsFailed { get; }
        
        public CommandStatus(
            string state, 
            string? message = null, 
            bool isExecutable = true, 
            bool isCompleted = false, 
            bool isFailed = false)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Message = message ?? string.Empty;
            LastUpdated = DateTime.UtcNow;
            IsExecutable = isExecutable;
            IsCompleted = isCompleted;
            IsFailed = isFailed;
        }
        
        public static ICommandStatus Ready(string message = "Command is ready to execute") => 
            new CommandStatus("Ready", message, true, false, false);
            
        public static ICommandStatus Executing(string message = "Command is executing") => 
            new CommandStatus("Executing", message, false, false, false);
            
        public static ICommandStatus Completed(string message = "Command completed successfully") => 
            new CommandStatus("Completed", message, false, true, false);
            
        public static ICommandStatus Failed(string message = "Command execution failed") => 
            new CommandStatus("Failed", message, false, false, true);
            
        public static ICommandStatus Cancelled(string message = "Command was cancelled") => 
            new CommandStatus("Cancelled", message, false, false, false);
    }
}
