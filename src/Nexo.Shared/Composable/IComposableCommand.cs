using System;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Core interface for composable, self-contained commands
    /// </summary>
    public interface IComposableCommand
    {
        /// <summary>
        /// Unique identifier for this command
        /// </summary>
        ICommandIdentifier Id { get; }
        
        /// <summary>
        /// Human-readable name of the command
        /// </summary>
        ICommandName Name { get; }
        
        /// <summary>
        /// Description of what this command does
        /// </summary>
        ICommandDescription Description { get; }
        
        /// <summary>
        /// Current status of the command
        /// </summary>
        ICommandStatus Status { get; }
        
        /// <summary>
        /// Dependencies required by this command
        /// </summary>
        ICommandDependencies Dependencies { get; }
        
        /// <summary>
        /// Capabilities provided by this command
        /// </summary>
        ICommandCapabilities Capabilities { get; }
        
        /// <summary>
        /// Execute the command with the given context
        /// </summary>
        Task<ICommandResult> ExecuteAsync(ICommandContext context);
        
        /// <summary>
        /// Validate if the command can execute with the given context
        /// </summary>
        Task<IValidationResult> ValidateAsync(ICommandContext context);
        
        /// <summary>
        /// Rollback the command if needed
        /// </summary>
        Task<ICommandResult> RollbackAsync(ICommandContext context);
    }
}
