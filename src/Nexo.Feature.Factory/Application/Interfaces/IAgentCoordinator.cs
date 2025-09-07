using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Interfaces
{
    /// <summary>
    /// Coordinates multiple specialized AI agents to work together on feature generation.
    /// </summary>
    public interface IAgentCoordinator
    {
        /// <summary>
        /// Coordinates the analysis of a feature specification using multiple agents.
        /// </summary>
        /// <param name="specification">The feature specification to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The enhanced specification with detailed analysis</returns>
        Task<FeatureSpecification> CoordinateAnalysisAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates the generation of code artifacts using multiple agents.
        /// </summary>
        /// <param name="specification">The feature specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated code artifacts</returns>
        Task<IReadOnlyList<CodeArtifact>> CoordinateGenerationAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates the validation of generated code using multiple agents.
        /// </summary>
        /// <param name="artifacts">The generated code artifacts</param>
        /// <param name="specification">The original specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation results</returns>
        Task<ValidationResult> CoordinateValidationAsync(IReadOnlyList<CodeArtifact> artifacts, FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of all registered agents.
        /// </summary>
        /// <returns>The status of all agents</returns>
        Task<IReadOnlyList<AgentStatus>> GetAgentStatusesAsync();

        /// <summary>
        /// Registers a new agent with the coordinator.
        /// </summary>
        /// <param name="agent">The agent to register</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters an agent from the coordinator.
        /// </summary>
        /// <param name="agentId">The ID of the agent to unregister</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the status of an agent.
    /// </summary>
    public sealed class AgentStatus
    {
        /// <summary>
        /// Gets the agent ID.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Gets the agent name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the agent status.
        /// </summary>
        public AgentState State { get; }

        /// <summary>
        /// Gets the last activity time.
        /// </summary>
        public DateTimeOffset LastActivity { get; }

        /// <summary>
        /// Gets any error messages.
        /// </summary>
        public string? ErrorMessage { get; }

        public AgentStatus(string agentId, string name, AgentState state, DateTimeOffset lastActivity, string? errorMessage = null)
        {
            AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            State = state;
            LastActivity = lastActivity;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Represents the state of an agent.
    /// </summary>
    public enum AgentState
    {
        Idle,
        Working,
        Error,
        Offline
    }

    /// <summary>
    /// Represents the result of validation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Gets the validation warnings.
        /// </summary>
        public IReadOnlyList<ValidationWarning> Warnings { get; }

        public ValidationResult(bool isValid, IReadOnlyList<ValidationError>? errors = null, IReadOnlyList<ValidationWarning>? warnings = null)
        {
            IsValid = isValid;
            Errors = errors ?? new List<ValidationError>();
            Warnings = warnings ?? new List<ValidationWarning>();
        }
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the artifact that has the error.
        /// </summary>
        public string? ArtifactName { get; }

        /// <summary>
        /// Gets the line number where the error occurs.
        /// </summary>
        public int? LineNumber { get; }

        public ValidationError(string message, string? artifactName = null, int? lineNumber = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ArtifactName = artifactName;
            LineNumber = lineNumber;
        }
    }

    /// <summary>
    /// Represents a validation warning.
    /// </summary>
    public sealed class ValidationWarning
    {
        /// <summary>
        /// Gets the warning message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the artifact that has the warning.
        /// </summary>
        public string? ArtifactName { get; }

        /// <summary>
        /// Gets the line number where the warning occurs.
        /// </summary>
        public int? LineNumber { get; }

        public ValidationWarning(string message, string? artifactName = null, int? lineNumber = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ArtifactName = artifactName;
            LineNumber = lineNumber;
        }
    }
}
