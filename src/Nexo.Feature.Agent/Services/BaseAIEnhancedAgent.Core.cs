using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Core properties and initialization for AI-enhanced agents
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent : IAiEnhancedAgent
    {
        /// <summary>
        /// Provides logging functionality for classes that inherit from the BaseAiEnhancedAgent.
        /// Used to log relevant information, such as the processing of requests or lifecycle events,
        /// aiding in debugging, monitoring, and tracing operations within AI-enhanced agents.
        /// </summary>
        protected readonly ILogger<BaseAiEnhancedAgent> Logger;

        /// <summary>
        /// A protected, readonly instance of the <see cref="IModelOrchestrator"/> interface utilized for managing and executing
        /// AI model-related operations within the <see cref="BaseAiEnhancedAgent"/>.
        /// </summary>
        /// <remarks>
        /// This variable acts as the central component for interactions with AI models, providing functionalities such as
        /// executing model requests and handling responses. It is initialized via dependency injection through the constructor
        /// and is used extensively in methods that involve AI processing, such as task analysis, suggestion generation, and
        /// AI-enhanced operations.
        /// </remarks>
        protected readonly IModelOrchestrator _modelOrchestrator;

        /// <summary>
        /// Represents the base implementation for AI-enhanced agent functionality.
        /// </summary>
        /// <remarks>
        /// This abstract class serves as the foundation for all AI-enhanced agent types, providing core properties
        /// and shared functionality such as agent identity, name, role, status, and capabilities.
        /// Derived classes can extend its behavior as required for specific agent roles.
        /// </remarks>
        protected BaseAiEnhancedAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            ILogger<BaseAiEnhancedAgent> logger)
        {
            Id = id;
            Name = name;
            Role = role;
            Status = AgentStatus.Inactive;
            _modelOrchestrator = modelOrchestrator;
            Logger = logger;
            
            Capabilities = new List<string>();
            FocusAreas = new List<string>();
            AiCapabilities = new AiAgentCapabilities();
        }

        /// <summary>
        /// Gets the unique identifier of the AI-enhanced agent.
        /// Represents an instance of <see cref="AgentId"/>, which serves as the agent's primary identifier.
        /// </summary>
        public AgentId Id { get; }

        /// <summary>
        /// Gets the name of the AI-enhanced agent.
        /// </summary>
        /// <remarks>
        /// This property represents the unique, human-readable name of the agent.
        /// The name is typically used for identification and logging purposes.
        /// </remarks>
        public AgentName Name { get; }

        /// <summary>
        /// Represents the functional role assigned to an AI-enhanced agent within the system.
        /// </summary>
        /// <remarks>
        /// The <c>Role</c> property is used to define the specific responsibilities or designation
        /// of an agent. It helps in tailoring the agent's behavior and capabilities based on its
        /// assigned role.
        /// </remarks>
        public AgentRole Role { get; }

        /// <summary>
        /// Represents the current operational state of the agent.
        /// </summary>
        /// <remarks>
        /// The status can be one of the following values defined in the AgentStatus enumeration:
        /// Inactive, Active, Busy, or Failed. The status transitions are managed internally
        /// based on the agent's activities, such as processing requests or encountering errors.
        /// </remarks>
        public AgentStatus Status { get; protected set; }

        /// <summary>
        /// Gets the list of capabilities specific to the AI-enhanced agent.
        /// </summary>
        /// <remarks>
        /// This property represents a collection of strings that define the set of functionalities
        /// or specializations associated with the agent. Derived classes can initialize and populate
        /// this list with relevant capabilities based on their specific roles or purposes.
        /// </remarks>
        public List<string> Capabilities { get; }

        /// <summary>
        /// A collection representing the primary focus areas or domains of expertise
        /// associated with this agent. This property outlines the key architectural
        /// domains or patterns the agent specializes in, such as "Cloud-Native Architecture"
        /// or "Domain-Driven Design". It provides an overview of the agent's specialization
        /// for informed decision-making or task assignments.
        /// </summary>
        public List<string> FocusAreas { get; }

        /// Represents a service responsible for handling and orchestrating interactions
        /// with underlying AI models. Provides functionality to process and fulfill
        /// model-related tasks requested by various agents within the system.
        /// Typically injected into agents or other services requiring AI-enhanced capabilities.
        /// The interface serves as an abstraction layer to encapsulate the behavior
        /// of different AI models or orchestration implementations, enabling flexibility
        /// and ease of integration across the system.
        public IModelOrchestrator ModelOrchestrator => _modelOrchestrator;

        /// <summary>
        /// Gets the AI capabilities associated with the agent.
        /// </summary>
        /// <remarks>
        /// Provides configurable capabilities for the AI agent, such as code analysis, task analysis,
        /// problem-solving, and more. This property allows customization of AI functionality based on the
        /// specific role and focus areas of the agent.
        /// </remarks>
        public AiAgentCapabilities AiCapabilities { get; }
    }
}
