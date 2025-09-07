using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Interfaces
{
    /// <summary>
    /// Base interface for all AI agents in the feature factory system.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Gets the unique identifier of the agent.
        /// </summary>
        string AgentId { get; }

        /// <summary>
        /// Gets the name of the agent.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the agent.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the capabilities of the agent.
        /// </summary>
        IReadOnlyList<AgentCapability> Capabilities { get; }

        /// <summary>
        /// Gets the current status of the agent.
        /// </summary>
        AgentState Status { get; }

        /// <summary>
        /// Initializes the agent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shuts down the agent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ShutdownAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a request and returns a response.
        /// </summary>
        /// <param name="request">The agent request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The agent response</returns>
        Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a capability of an agent.
    /// </summary>
    public sealed class AgentCapability
    {
        /// <summary>
        /// Gets the name of the capability.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the capability.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the input type for this capability.
        /// </summary>
        public string InputType { get; }

        /// <summary>
        /// Gets the output type for this capability.
        /// </summary>
        public string OutputType { get; }

        public AgentCapability(string name, string description, string inputType, string outputType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            InputType = inputType ?? throw new ArgumentNullException(nameof(inputType));
            OutputType = outputType ?? throw new ArgumentNullException(nameof(outputType));
        }
    }

    /// <summary>
    /// Represents a request to an agent.
    /// </summary>
    public sealed class AgentRequest
    {
        /// <summary>
        /// Gets the request ID.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public string RequestType { get; }

        /// <summary>
        /// Gets the request data.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets the request metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; }

        public AgentRequest(string requestId, string requestType, object data, IReadOnlyDictionary<string, object>? metadata = null)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            Data = data;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Represents a response from an agent.
    /// </summary>
    public sealed class AgentResponse
    {
        /// <summary>
        /// Gets the request ID this response corresponds to.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets the response data.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets whether the response was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets any error messages.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the response metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; }

        public AgentResponse(string requestId, object data, bool isSuccess, string? errorMessage = null, IReadOnlyDictionary<string, object>? metadata = null)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Data = data;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }
}
