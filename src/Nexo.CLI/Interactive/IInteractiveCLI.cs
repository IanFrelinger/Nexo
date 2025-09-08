using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Interface for interactive CLI functionality with intelligent suggestions and guided workflows
    /// </summary>
    public interface IInteractiveCLI
    {
        /// <summary>
        /// Starts the interactive CLI mode with intelligent suggestions and guided workflows
        /// </summary>
        Task StartInteractiveModeAsync();
        
        /// <summary>
        /// Processes a command in interactive mode with enhanced output and progress tracking
        /// </summary>
        Task ProcessInteractiveCommandAsync(string command);
        
        /// <summary>
        /// Shows contextual help based on current state and available commands
        /// </summary>
        Task ShowContextualHelpAsync();
        
        /// <summary>
        /// Displays the current system status and context
        /// </summary>
        Task ShowSystemStatusAsync();
    }
}
