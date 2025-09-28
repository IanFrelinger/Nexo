using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents the complete state of an agent for persistence and recovery
    /// </summary>
    public sealed class AgentState
    {
        public AgentId Id { get; }
        public AgentName Name { get; }
        public AgentStatus Status { get; }
        public PlatformType Platform { get; }
        public IReadOnlyList<AgentCapability> Capabilities { get; }
        public IReadOnlyList<string> FocusAreas { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? LastActivatedAt { get; }
        public DateTimeOffset? LastDeactivatedAt { get; }
        public string? FailureReason { get; }
        public IReadOnlyList<AgentExperience> RecentExperiences { get; }
        public IReadOnlyDictionary<string, object> RuntimeData { get; }
        public IReadOnlyList<string> ActiveTasks { get; }
        public IReadOnlyList<string> PendingMessages { get; }
        public DateTimeOffset LastUpdatedAt { get; }
        public int Version { get; }

        public AgentState(
            AgentId id,
            AgentName name,
            AgentStatus status,
            PlatformType platform,
            IEnumerable<AgentCapability>? capabilities = null,
            IEnumerable<string>? focusAreas = null,
            IDictionary<string, object>? configuration = null,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? lastActivatedAt = null,
            DateTimeOffset? lastDeactivatedAt = null,
            string? failureReason = null,
            IEnumerable<AgentExperience>? recentExperiences = null,
            IDictionary<string, object>? runtimeData = null,
            IEnumerable<string>? activeTasks = null,
            IEnumerable<string>? pendingMessages = null,
            DateTimeOffset? lastUpdatedAt = null,
            int version = 1)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Capabilities = (capabilities?.ToList() ?? new List<AgentCapability>()).AsReadOnly();
            FocusAreas = (focusAreas?.ToList() ?? new List<string>()).AsReadOnly();
            Configuration = (configuration?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
            LastActivatedAt = lastActivatedAt;
            LastDeactivatedAt = lastDeactivatedAt;
            FailureReason = failureReason;
            RecentExperiences = (recentExperiences?.ToList() ?? new List<AgentExperience>()).AsReadOnly();
            RuntimeData = (runtimeData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            ActiveTasks = (activeTasks?.ToList() ?? new List<string>()).AsReadOnly();
            PendingMessages = (pendingMessages?.ToList() ?? new List<string>()).AsReadOnly();
            LastUpdatedAt = lastUpdatedAt ?? DateTimeOffset.UtcNow;
            Version = version;
        }

        /// <summary>
        /// Create agent state from an agent instance
        /// </summary>
        public static AgentState FromAgent(IAgent agent)
        {
            return new AgentState(
                agent.Id,
                agent.Name,
                agent.Status,
                agent.Platform,
                agent.Capabilities,
                agent.FocusAreas,
                agent.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>
        /// Create agent state with updated status
        /// </summary>
        public AgentState WithStatus(AgentStatus newStatus)
        {
            return new AgentState(
                Id,
                Name,
                newStatus,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                newStatus == AgentStatus.Active ? DateTimeOffset.UtcNow : LastActivatedAt,
                newStatus == AgentStatus.Inactive ? DateTimeOffset.UtcNow : LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated capabilities
        /// </summary>
        public AgentState WithCapabilities(IEnumerable<AgentCapability> newCapabilities)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                newCapabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated focus areas
        /// </summary>
        public AgentState WithFocusAreas(IEnumerable<string> newFocusAreas)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                newFocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated configuration
        /// </summary>
        public AgentState WithConfiguration(IDictionary<string, object> newConfiguration)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                newConfiguration,
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with added experience
        /// </summary>
        public AgentState WithExperience(AgentExperience experience)
        {
            var updatedExperiences = new List<AgentExperience>(RecentExperiences) { experience };
            // Keep only the most recent 100 experiences
            if (updatedExperiences.Count > 100)
            {
                updatedExperiences = updatedExperiences
                    .OrderByDescending(e => e.Timestamp)
                    .Take(100)
                    .ToList();
            }

            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                updatedExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated runtime data
        /// </summary>
        public AgentState WithRuntimeData(IDictionary<string, object> newRuntimeData)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                newRuntimeData,
                ActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated active tasks
        /// </summary>
        public AgentState WithActiveTasks(IEnumerable<string> newActiveTasks)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                newActiveTasks,
                PendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Create agent state with updated pending messages
        /// </summary>
        public AgentState WithPendingMessages(IEnumerable<string> newPendingMessages)
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreatedAt,
                LastActivatedAt,
                LastDeactivatedAt,
                FailureReason,
                RecentExperiences,
                RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ActiveTasks,
                newPendingMessages,
                DateTimeOffset.UtcNow,
                Version + 1);
        }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        public T? GetConfigurationValue<T>(string key)
        {
            if (Configuration.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Get a runtime data value
        /// </summary>
        public T? GetRuntimeDataValue<T>(string key)
        {
            if (RuntimeData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Check if agent is active
        /// </summary>
        public bool IsActive => Status == AgentStatus.Active;

        /// <summary>
        /// Check if agent is busy
        /// </summary>
        public bool IsBusy => Status == AgentStatus.Busy;

        /// <summary>
        /// Check if agent has failed
        /// </summary>
        public bool HasFailed => Status == AgentStatus.Failed;

        /// <summary>
        /// Check if agent has active tasks
        /// </summary>
        public bool HasActiveTasks => ActiveTasks.Count > 0;

        /// <summary>
        /// Check if agent has pending messages
        /// </summary>
        public bool HasPendingMessages => PendingMessages.Count > 0;

        /// <summary>
        /// Check if agent has recent experiences
        /// </summary>
        public bool HasRecentExperiences => RecentExperiences.Count > 0;

        /// <summary>
        /// Get the uptime of the agent
        /// </summary>
        public TimeSpan GetUptime()
        {
            if (Status == AgentStatus.Active && LastActivatedAt.HasValue)
            {
                return DateTimeOffset.UtcNow - LastActivatedAt.Value;
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Get the downtime of the agent
        /// </summary>
        public TimeSpan GetDowntime()
        {
            if (Status == AgentStatus.Inactive && LastDeactivatedAt.HasValue)
            {
                return DateTimeOffset.UtcNow - LastDeactivatedAt.Value;
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Get a summary of the agent state
        /// </summary>
        public string GetSummary()
        {
            var summary = $"{Name} ({Status}) on {Platform}";
            if (HasActiveTasks)
            {
                summary += $" - {ActiveTasks.Count} active tasks";
            }
            if (HasPendingMessages)
            {
                summary += $" - {PendingMessages.Count} pending messages";
            }
            if (HasRecentExperiences)
            {
                summary += $" - {RecentExperiences.Count} recent experiences";
            }
            return summary;
        }
    }
}
