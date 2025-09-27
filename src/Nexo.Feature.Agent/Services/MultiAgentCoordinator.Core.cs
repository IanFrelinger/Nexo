using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Core multi-agent coordinator functionality
    /// </summary>
    public partial class MultiAgentCoordinator : IMultiAgentCoordinator
    {
        /// <summary>
        /// Logger instance used for recording diagnostic messages, tracking events,
        /// and executing error logging within the <see cref="MultiAgentCoordinator"/> class.
        /// </summary>
        /// <remarks>
        /// This logger is specifically typed for <see cref="MultiAgentCoordinator"/>
        /// and is utilized to capture key operation details such as agent registrations,
        /// collaboration sessions, and error occurrences.
        /// </remarks>
        private readonly ILogger<MultiAgentCoordinator> _logger;

        /// <summary>
        /// Represents the dependency used to manage and orchestrate AI model interactions within the multi-agent system.
        /// </summary>
        /// <remarks>
        /// The IModelOrchestrator implementation provides methods for working with AI model providers, enabling tasks such as model execution and retrieval of supported model information.
        /// </remarks>
        private readonly IModelOrchestrator _modelOrchestrator;

        /// <summary>
        /// A private dictionary that maps agent identifiers (as strings) to their corresponding
        /// IAiEnhancedAgent implementations. It is used within the MultiAgentCoordinator class
        /// to keep track of agents currently registered in the system.
        /// </summary>
        private readonly Dictionary<string, IAiEnhancedAgent> _registeredAgents;

        /// <summary>
        /// A private dictionary that stores the capability profiles of registered agents.
        /// The key represents the unique identifier of the agent, and the value is an
        /// <see cref="AgentCapabilityProfile"/> object containing details about the agent's
        /// capabilities, roles, and focus areas. This dictionary is utilized to manage,
        /// access, and update the capabilities of registered agents within the
        /// <see cref="MultiAgentCoordinator"/>.
        /// </summary>
        private readonly Dictionary<string, AgentCapabilityProfile> _agentCapabilities;

        /// <summary>
        /// Maintains the list of active collaboration sessions within the multi-agent coordination system.
        /// This collection is used to track and manage ongoing collaboration sessions between agents,
        /// facilitating seamless communication and task execution.
        /// </summary>
        /// <remarks>
        /// Each session in the list represents a specific collaboration activity involving one or more agents.
        /// The collection is updated dynamically as sessions are created, modified, or completed.
        /// </remarks>
        private readonly List<CollaborationSession> _activeSessions;

        /// <summary>
        /// Object used for synchronizing access to shared resources related to agent registration,
        /// collaboration, and management within the MultiAgentCoordinator class.
        /// </summary>
        /// <remarks>
        /// Prevents race conditions when modifying or accessing shared agent-related collections
        /// or performing operations requiring thread safety.
        /// </remarks>
        private readonly object _agentsLock = new object();

        /// <summary>
        /// Coordinates interactions and collaborations among multiple agents, enabling agent-to-agent communication, task execution, and capability management.
        /// </summary>
        public MultiAgentCoordinator(
            IModelOrchestrator modelOrchestrator,
            ILogger<MultiAgentCoordinator> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _registeredAgents = new Dictionary<string, IAiEnhancedAgent>();
            _agentCapabilities = new Dictionary<string, AgentCapabilityProfile>();
            _activeSessions = new List<CollaborationSession>();
        }

        /// <summary>
        /// Registers an agent with the coordinator, enabling it to participate in multi-agent collaboration and communication workflows.
        /// </summary>
        /// <param name="agent">The agent to be registered, implementing the IAiEnhancedAgent interface.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided agent is null.</exception>
        public void RegisterAgent(IAiEnhancedAgent agent)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            
            lock (_agentsLock)
            {
                var agentId = agent.Id.Value;
                _registeredAgents[agentId] = agent;
                
                // Create capability profile
                _agentCapabilities[agentId] = new AgentCapabilityProfile
                {
                    AgentId = agentId,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Capabilities = agent.Capabilities.ToList(),
                    FocusAreas = agent.FocusAreas.ToList(),
                    AiCapabilities = agent.AiCapabilities
                };
                
                _logger.LogInformation("Registered agent: {AgentName} ({AgentId})", agent.Name.Value, agentId);
            }
        }

        /// <summary>
        /// Unregisters an agent from the coordinator.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent to be unregistered.</param>
        public void UnregisterAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId)) throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            
            lock (_agentsLock)
            {
                if (!_registeredAgents.Remove(agentId)) return;
                _agentCapabilities.Remove(agentId);
                _logger.LogInformation("Unregistered agent: {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Retrieves a list of all currently registered agents.
        /// </summary>
        /// <returns>
        /// A list of registered agents implementing the IAiEnhancedAgent interface.
        /// </returns>
        public List<IAiEnhancedAgent> GetRegisteredAgents()
        {
            lock (_agentsLock)
            {
                return _registeredAgents.Values.ToList();
            }
        }

        /// <summary>
        /// Retrieves the capability profile of a specified agent.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose capabilities are being retrieved.</param>
        /// <returns>The capability profile of the specified agent. Returns a default profile if the agent is not found.</returns>
        public AgentCapabilityProfile GetAgentCapabilities(string agentId)
        {
            lock (_agentsLock)
            {
                return _agentCapabilities.ContainsKey(agentId) ? _agentCapabilities[agentId] : new AgentCapabilityProfile();
            }
        }

        /// <summary>
        /// Retrieves a list of active collaboration sessions.
        /// </summary>
        /// <returns>
        /// A list of active CollaborationSession objects that are currently ongoing.
        /// </returns>
        public List<CollaborationSession> GetActiveSessions()
        {
            lock (_agentsLock)
            {
                return _activeSessions.Where(s => s.Status == CollaborationSessionStatus.Active).ToList();
            }
        }
    }
}
