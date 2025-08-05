using System.Collections.Generic;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Represents a request made to the agent, containing details about the type of request, context, and content.
    /// </summary>
    public class AgentRequest
    {
        /// <summary>
        /// Gets or sets the type of the agent request.
        /// </summary>
        /// <remarks>
        /// This property specifies the category or nature of the agent request, as defined by the
        /// <see cref="AgentRequestType"/> enumeration. It determines how the request will be processed.
        /// </remarks>
        /// <value>
        /// A value of type <see cref="AgentRequestType"/> representing the type of request, such as
        /// CodeReview, BugFix, FeatureImplementation, and so on.
        /// </value>
        public AgentRequestType Type { get; set; }

        /// <summary>
        /// Represents additional contextual information associated with the agent request.
        /// This property holds a collection of key-value pairs, where the keys are strings
        /// and the values are objects, allowing diverse data to be passed for processing.
        /// </summary>
        /// <remarks>
        /// The context can be used by AI agents or other components to process the
        /// request effectively based on the provided data. Typical use cases might include
        /// passing associated metadata such as code snippets, error messages, or configuration
        /// settings required for handling specific request types, such as bug fixes or code reviews.
        /// </remarks>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Represents the content of a request, typically used to provide
        /// detailed information or data necessary for processing by an agent.
        /// This property is often utilized in scenarios where dynamic or context-specific
        /// communication between agents or systems is required.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}