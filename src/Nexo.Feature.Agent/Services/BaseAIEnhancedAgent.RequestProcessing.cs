using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Core request processing functionality for AI-enhanced agents
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Processes a given agent request asynchronously, updating the agent's status throughout the operation.
        /// </summary>
        /// <param name="request">The agent request to process.</param>
        /// <param name="ct">The cancellation token to observe for cancellation requests.</param>
        /// <returns>Returns the response generated from processing the agent request.</returns>
        public virtual async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            Logger.LogInformation("Processing request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            try
            {
                Status = AgentStatus.Busy;
                
                var response = await ProcessRequestInternalAsync(request, ct);
                
                Status = AgentStatus.Active;
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                throw;
            }
        }

        /// <summary>
        /// Processes an internal request asynchronously. This method is designed to be implemented by derived classes
        /// to handle the specifics of request processing based on the agent's functionality.
        /// </summary>
        /// <param name="request">The request to be processed, represented by an instance of <see cref="AgentRequest"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to observe cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an instance of <see cref="AgentResponse"/>
        /// which represents the outcome of processing the request.
        /// </returns>
        protected abstract Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken ct);
    }
}
