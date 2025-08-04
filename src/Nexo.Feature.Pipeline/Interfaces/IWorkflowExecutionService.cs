using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Service for executing development workflows including setup, analyze, test, and deploy.
    /// </summary>
    public interface IWorkflowExecutionService
    {
        /// <summary>
        /// Executes a development workflow.
        /// </summary>
        /// <param name="type">Type of workflow to execute.</param>
        /// <param name="projectPath">Path to the project directory.</param>
        /// <param name="configPath">Optional path to workflow configuration file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the workflow execution.</returns>
        Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
            WorkflowType type,
            string projectPath,
            string configPath = null,
            CancellationToken cancellationToken = default);
    }
}