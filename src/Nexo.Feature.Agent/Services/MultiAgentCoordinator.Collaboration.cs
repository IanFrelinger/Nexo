using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Collaboration session management functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Creates a collaboration session between multiple agents.
        /// </summary>
        /// <param name="request">The collaboration request containing details for session creation.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the created collaboration session.</returns>
        /// <exception cref="System.Exception">Thrown when an error occurs during the creation of the collaboration session.</exception>
        public async Task<CollaborationSession> CreateCollaborationSessionAsync(CollaborationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating collaboration session: {SessionName}", request.SessionName);

            try
            {
                // Select agents based on requirements
                var selectedAgents = await SelectAgentsForCollaborationAsync(request, cancellationToken);
                
                if (!selectedAgents.Any())
                {
                    throw new InvalidOperationException("No suitable agents found for the collaboration request");
                }

                var session = new CollaborationSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    SessionName = request.SessionName,
                    Description = request.Description,
                    ParticipatingAgents = selectedAgents,
                    SessionType = request.SessionType,
                    Status = CollaborationSessionStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    Configuration = request.Configuration
                };

                lock (_agentsLock)
                {
                    _activeSessions.Add(session);
                }

                _logger.LogInformation("Created collaboration session {SessionId} with {AgentCount} agents", 
                    session.SessionId, selectedAgents.Count);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collaboration session");
                throw;
            }
        }
    }
}
