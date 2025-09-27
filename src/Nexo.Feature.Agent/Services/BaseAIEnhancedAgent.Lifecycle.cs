using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Agent lifecycle management functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Starts the AI-enhanced agent and sets its status to active. Executes any additional start logic defined in the derived class.
        /// </summary>
        /// <param name="ct">A CancellationToken to observe while waiting for the operation's completion.</param>
        /// <returns>A task representing the asynchronous start operation.</returns>
        public virtual async Task StartAsync(CancellationToken ct)
        {
            Logger.LogInformation("Starting AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Active;
            await OnStartedAsync(ct);
        }

        /// <summary>
        /// Asynchronously stops the AI-enhanced agent and performs necessary cleanup operations.
        /// Updates the agent's status to inactive and triggers the OnStoppedAsync method for additional handling.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to signal the stop operation should be canceled.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous stop operation.</returns>
        public virtual async Task StopAsync(CancellationToken ct)
        {
            Logger.LogInformation("Stopping AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Inactive;
            await OnStoppedAsync(ct);
        }

        /// <summary>
        /// Invoked when the AI-enhanced agent starts, allowing for any necessary initializations or task preparations.
        /// This method must be implemented by derived classes to define specific startup logic.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to observe cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected abstract Task OnStartedAsync(CancellationToken ct);

        /// <summary>
        /// Performs actions required when the AI-enhanced agent is stopped.
        /// This method is invoked during the stop lifecycle of the agent to handle cleanup or additional stopping logic.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> that propagates notification that the stop operation should be canceled.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected abstract Task OnStoppedAsync(CancellationToken ct);
    }
}
