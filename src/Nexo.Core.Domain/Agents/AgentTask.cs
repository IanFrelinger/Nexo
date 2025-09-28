using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents a task that can be executed by an agent
    /// </summary>
    public sealed class AgentTask
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public TaskType Type { get; }
        public TaskPriority Priority { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public IReadOnlyList<string> RequiredCapabilities { get; }
        public IReadOnlyList<string> RequiredResources { get; }
        public TimeSpan? Timeout { get; }
        public AgentId? AssignedAgent { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? Deadline { get; }

        public AgentTask(
            string id,
            string name,
            string description,
            TaskType type,
            TaskPriority? priority = null,
            IDictionary<string, object>? parameters = null,
            IEnumerable<string>? requiredCapabilities = null,
            IEnumerable<string>? requiredResources = null,
            TimeSpan? timeout = null,
            AgentId? assignedAgent = null,
            DateTimeOffset? deadline = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type;
            Priority = priority ?? TaskPriority.Medium;
            Parameters = (parameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            RequiredCapabilities = (requiredCapabilities?.ToList() ?? new List<string>()).AsReadOnly();
            RequiredResources = (requiredResources?.ToList() ?? new List<string>()).AsReadOnly();
            Timeout = timeout;
            AssignedAgent = assignedAgent;
            CreatedAt = DateTimeOffset.UtcNow;
            Deadline = deadline;
        }

        /// <summary>
        /// Create a code generation task
        /// </summary>
        public static AgentTask CreateCodeGenerationTask(
            string id,
            string name,
            string description,
            string language,
            string framework,
            TaskPriority? priority = null,
            TimeSpan? timeout = null)
        {
            return new AgentTask(
                id,
                name,
                description,
                TaskType.CodeGeneration,
                priority ?? TaskPriority.Medium,
                new Dictionary<string, object>
                {
                    ["language"] = language,
                    ["framework"] = framework
                },
                new[] { "Code Generation" },
                new[] { "CPU", "Memory" },
                timeout);
        }

        /// <summary>
        /// Create a code analysis task
        /// </summary>
        public static AgentTask CreateCodeAnalysisTask(
            string id,
            string name,
            string description,
            string filePath,
            string analysisType,
            TaskPriority? priority = null,
            TimeSpan? timeout = null)
        {
            return new AgentTask(
                id,
                name,
                description,
                TaskType.CodeAnalysis,
                priority ?? TaskPriority.Medium,
                new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["analysisType"] = analysisType
                },
                new[] { "Code Analysis" },
                new[] { "CPU", "Memory" },
                timeout);
        }

        /// <summary>
        /// Create a cross-platform deployment task
        /// </summary>
        public static AgentTask CreateDeploymentTask(
            string id,
            string name,
            string description,
            string[] targetPlatforms,
            string deploymentType,
            TaskPriority? priority = null,
            TimeSpan? timeout = null)
        {
            return new AgentTask(
                id,
                name,
                description,
                TaskType.Deployment,
                priority ?? TaskPriority.Medium,
                new Dictionary<string, object>
                {
                    ["targetPlatforms"] = targetPlatforms,
                    ["deploymentType"] = deploymentType
                },
                new[] { "Cross-Platform Deployment" },
                new[] { "CPU", "Memory", "Network" },
                timeout);
        }

        /// <summary>
        /// Create a security analysis task
        /// </summary>
        public static AgentTask CreateSecurityAnalysisTask(
            string id,
            string name,
            string description,
            string targetPath,
            string analysisScope,
            TaskPriority? priority = null,
            TimeSpan? timeout = null)
        {
            return new AgentTask(
                id,
                name,
                description,
                TaskType.SecurityAnalysis,
                priority ?? TaskPriority.High,
                new Dictionary<string, object>
                {
                    ["targetPath"] = targetPath,
                    ["analysisScope"] = analysisScope
                },
                new[] { "Security Analysis" },
                new[] { "CPU", "Memory" },
                timeout);
        }

        /// <summary>
        /// Create a communication task
        /// </summary>
        public static AgentTask CreateCommunicationTask(
            string id,
            string name,
            string description,
            AgentId targetAgent,
            string messageType,
            TaskPriority? priority = null,
            TimeSpan? timeout = null)
        {
            return new AgentTask(
                id,
                name,
                description,
                TaskType.Communication,
                priority ?? TaskPriority.Medium,
                new Dictionary<string, object>
                {
                    ["targetAgent"] = targetAgent.ToString(),
                    ["messageType"] = messageType
                },
                new[] { "Agent Communication" },
                new[] { "CPU", "Memory", "Network" },
                timeout);
        }

        /// <summary>
        /// Check if this task can be executed by the given agent
        /// </summary>
        public bool CanBeExecutedBy(IAgent agent)
        {
            // Check if agent has required capabilities
            var agentCapabilities = agent.Capabilities.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (RequiredCapabilities.Any(required => !agentCapabilities.Contains(required)))
                return false;

            // Check if agent is available
            if (agent.Status != AgentStatus.Active)
                return false;

            // Check if task is assigned to this agent or no specific agent
            if (AssignedAgent != null && !AssignedAgent.Equals(agent.Id))
                return false;

            return true;
        }

        /// <summary>
        /// Get a parameter value
        /// </summary>
        public T? GetParameter<T>(string key)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Check if task has expired
        /// </summary>
        public bool IsExpired => Deadline.HasValue && DateTimeOffset.UtcNow > Deadline.Value;

        /// <summary>
        /// Check if task is urgent
        /// </summary>
        public bool IsUrgent => Priority == TaskPriority.Critical || 
                               (Deadline.HasValue && DateTimeOffset.UtcNow.AddHours(1) > Deadline.Value);
    }

    /// <summary>
    /// Types of tasks that agents can execute
    /// </summary>
    public enum TaskType
    {
        CodeGeneration,
        CodeAnalysis,
        Deployment,
        SecurityAnalysis,
        Communication,
        DataProcessing,
        UserInterfaceDesign,
        Infrastructure,
        Testing,
        Documentation,
        Custom
    }
}
