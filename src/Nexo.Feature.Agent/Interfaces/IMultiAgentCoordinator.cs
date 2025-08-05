using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Interfaces
{
    /// <summary>
    /// Multi-agent coordinator interface for agent-to-agent communication and collaboration.
    /// </summary>
    public interface IMultiAgentCoordinator
    {
        /// <summary>
        /// Registers an agent with the coordinator.
        /// </summary>
        /// <param name="agent">The agent to register.</param>
        void RegisterAgent(IAiEnhancedAgent agent);

        /// <summary>
        /// Unregisters an agent from the coordinator.
        /// </summary>
        /// <param name="agentId">The ID of the agent to unregister.</param>
        void UnregisterAgent(string agentId);

        /// <summary>
        /// Creates a collaboration session between multiple agents.
        /// </summary>
        /// <param name="request">The collaboration request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created collaboration session.</returns>
        Task<CollaborationSession> CreateCollaborationSessionAsync(CollaborationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a collaborative task with multiple agents.
        /// </summary>
        /// <param name="task">The collaborative task to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collaboration result with agent responses.</returns>
        Task<CollaborationResult> ExecuteCollaborativeTaskAsync(CollaborativeTask task, CancellationToken cancellationToken = default);

        /// <summary>
        /// Facilitates agent-to-agent communication.
        /// </summary>
        /// <param name="request">The communication request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The communication result.</returns>
        Task<AgentCommunicationResult> FacilitateCommunicationAsync(AgentCommunicationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes collaboration patterns and provides insights.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collaboration analysis with patterns and recommendations.</returns>
        Task<CollaborationAnalysisResult> AnalyzeCollaborationPatternsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all registered agents.
        /// </summary>
        /// <returns>List of registered agents.</returns>
        List<IAiEnhancedAgent> GetRegisteredAgents();

        /// <summary>
        /// Gets agent capabilities by ID.
        /// </summary>
        /// <param name="agentId">The agent ID.</param>
        /// <returns>The agent capability profile.</returns>
        AgentCapabilityProfile GetAgentCapabilities(string agentId);

        /// <summary>
        /// Gets active collaboration sessions.
        /// </summary>
        /// <returns>List of active collaboration sessions.</returns>
        List<CollaborationSession> GetActiveSessions();
    }
} 