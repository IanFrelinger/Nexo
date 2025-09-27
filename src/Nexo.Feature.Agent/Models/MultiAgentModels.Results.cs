using System;
using System.Collections.Generic;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Result and metrics models for multi-agent systems
    /// </summary>
    public partial class MultiAgentModels
    {
        // Result models are defined in separate files
    }

    /// <summary>
    /// Result of a collaborative task execution.
    /// </summary>
    public class CollaborationResult
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the collaboration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the collaboration failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the individual agent results.
        /// </summary>
        public List<AgentTaskResult> AgentResults { get; set; } = new List<AgentTaskResult>();

        /// <summary>
        /// Gets or sets the synthesized result from all agents.
        /// </summary>
        public string SynthesizedResult { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collaboration metrics.
        /// </summary>
        public CollaborationMetrics CollaborationMetrics { get; set; } = new CollaborationMetrics(0.0, 0.0m);

        /// <summary>
        /// Gets or sets the collaboration metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of an individual agent's task execution.
    /// </summary>
    public class AgentTaskResult
    {
        /// <summary>
        /// Gets or sets the agent ID.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the agent name.
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the agent role.
        /// </summary>
        public string AgentRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the agent's task was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the agent's response content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the agent's task failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets whether AI was used by the agent.
        /// </summary>
        public bool AiWasUsed { get; set; }

        /// <summary>
        /// Gets or sets the AI model used by the agent.
        /// </summary>
        public string AiModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence score of the agent's response.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Gets or sets the agent's task metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metrics for collaboration performance.
    /// </summary>
    public class CollaborationMetrics
    {
        public CollaborationMetrics(double successRate, decimal totalCost)
        {
            SuccessRate = successRate;
            TotalCost = totalCost;
        }

        /// <summary>
        /// Gets or sets the total processing time in milliseconds.
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the number of agents involved.
        /// </summary>
        public int AgentCount { get; set; }

        /// <summary>
        /// Gets or sets the success rate of agent tasks.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the total cost of the collaboration.
        /// </summary>
        public decimal TotalCost { get; set; }
    }
}
