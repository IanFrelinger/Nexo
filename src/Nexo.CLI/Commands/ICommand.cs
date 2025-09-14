using System.Threading;
using System.Threading.Tasks;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Interface for CLI commands
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes the command with the given arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the command execution</returns>
        Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
    }
}
