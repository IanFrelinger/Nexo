using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Domain.Entities
{
    /// <summary>
    /// Represents an agent within the system, encompassing its attributes and lifecycle operations.
    /// </summary>
    /// <remarks>
    /// An agent is a domain entity with states, roles, and capabilities, designed for various operations
    /// and configurations within the system. The class provides methods to manage the agent's status,
    /// focus areas, and capabilities, as well as maintain configuration key-value pairs.
    /// </remarks>
    public sealed class Agent
    {
        /// <summary>
        /// Stores a collection of focus areas associated with the agent.
        /// </summary>
        private readonly List<string> _focusAreas = [];

        /// <summary>
        /// Stores the list of capabilities associated with the agent.
        /// </summary>
        private readonly List<string> _capabilities = [];

        /// <summary>
        /// Stores agent-specific configuration settings as key-value pairs.
        /// </summary>
        private readonly Dictionary<string, object> _configuration = new();

        /// <summary>
        /// Gets the unique identifier for the agent entity.
        /// </summary>
        public AgentId Id { get; private set; }

        /// <summary>
        /// Gets or sets the name of the agent.
        /// </summary>
        public AgentName Name { get; private set; }

        /// <summary>
        /// Gets or sets the role assigned to the agent, defining its responsibilities and permissions within the system.
        /// </summary>
        public AgentRole Role { get; private set; }

        /// <summary>
        /// Gets the list of focus areas associated with the agent.
        /// </summary>
        public IReadOnlyList<string> FocusAreas => _focusAreas.AsReadOnly();

        /// <summary>
        /// Gets the collection of capabilities associated with the agent.
        /// </summary>
        /// <remarks>
        /// Capabilities represent the skills, attributes, or functionalities that the agent possesses.
        /// </remarks>
        public IReadOnlyList<string> Capabilities => _capabilities.AsReadOnly();

        /// <summary>
        /// Gets the operational status of the agent.
        /// </summary>
        public AgentStatus Status { get; private set; }

        /// <summary>
        /// Gets the configuration settings associated with the agent as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, object> Configuration => new ReadOnlyDictionary<string, object>(_configuration);

        /// <summary>
        /// Gets the timestamp indicating when the agent was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Gets the timestamp indicating when the agent was last activated.
        /// </summary>
        public DateTimeOffset? LastActivatedAt { get; private set; }

        /// <summary>
        /// Gets the reason for the agent's failure when its status is set to Failed.
        /// </summary>
        public string? FailureReason { get; private set; }

        /// <summary>
        /// Represents an agent in the system, encapsulating its identity, name, role, status, focus areas, and other related attributes.
        /// </summary>
        public Agent(AgentId id, AgentName name, AgentRole role)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(role);

            Id = id;
            Name = name;
            Role = role;
            Status = AgentStatus.Inactive;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Activates the agent.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the agent is in the failed status.
        /// </exception>
        public void Activate()
        {
            if (Status == AgentStatus.Failed)
                throw new InvalidOperationException($"Cannot activate failed agent. Current failure: {FailureReason}");

            Status = AgentStatus.Active;
            LastActivatedAt = DateTimeOffset.UtcNow;
            FailureReason = null;
        }

        /// <summary>
        /// Deactivates the agent, setting its status to inactive.
        /// </summary>
        public void Deactivate()
        {
            Status = AgentStatus.Inactive;
        }

        /// <summary>
        /// Sets the agent's status to busy if the agent is currently active.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the agent's current status is not active.
        /// </exception>
        public void SetBusy()
        {
            if (Status != AgentStatus.Active)
                throw new InvalidOperationException($"Cannot set busy status. Agent must be active. Current status: {Status}");

            Status = AgentStatus.Busy;
        }

        /// <summary>
        /// Sets the agent status to fail.
        /// </summary>
        /// <param name="reason">The reason for the agent's failure.</param>
        /// <exception cref="ArgumentException">Thrown if the reason is null, empty, or whitespace.</exception>
        public void SetFailed(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Value cannot be null or whitespace", nameof(reason));

            Status = AgentStatus.Failed;
            FailureReason = reason;
        }

        /// <summary>
        /// Adds a focus area to the agent.
        /// </summary>
        /// <param name="focusArea">The focus area to add.</param>
        /// <exception cref="ArgumentException">Thrown when the focus area is null, empty, or whitespace.</exception>
        public void AddFocusArea(string focusArea)
        {
            if (string.IsNullOrWhiteSpace(focusArea))
                throw new ArgumentException("Value cannot be null or whitespace", nameof(focusArea));

            if (!_focusAreas.Contains(focusArea, StringComparer.OrdinalIgnoreCase))
            {
                _focusAreas.Add(focusArea);
            }
        }

        /// <summary>
        /// Removes a focus area from the agent.
        /// </summary>
        /// <param name="focusArea">The focus area to remove.</param>
        /// <returns>True if the focus area was successfully removed; otherwise, false.</returns>
        public bool RemoveFocusArea(string focusArea)
        {
            return _focusAreas.RemoveAll(f => f.Equals(focusArea, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Adds a capability to the agent.
        /// </summary>
        /// <param name="capability">The capability to add.</param>
        /// <exception cref="ArgumentException">Thrown when capability is null or empty.</exception>
        public void AddCapability(string capability)
        {
            if (string.IsNullOrWhiteSpace(capability))
                throw new ArgumentException("Value cannot be null or whitespace", nameof(capability));

            if (!_capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            {
                _capabilities.Add(capability);
            }
        }

        /// <summary>
        /// Removes a capability from the agent.
        /// </summary>
        /// <param name="capability">The ability to remove.</param>
        /// <returns>True if the capability was successfully removed; otherwise, false.</returns>
        public bool RemoveCapability(string capability)
        {
            return _capabilities.RemoveAll(c => c.Equals(capability, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Sets a configuration value for the agent.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The configuration value.</param>
        /// <exception cref="ArgumentException">Thrown when the key is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        public void SetConfiguration(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace", nameof(key));
            ArgumentNullException.ThrowIfNull(value);

            _configuration[key] = value;
        }

        /// <summary>
        /// Gets a configuration value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type of the configuration value.</typeparam>
        /// <param name="key">The key associated with the desired configuration value.</param>
        /// <returns>The configuration value if found and can be converted to type <typeparamref name="T"/>; otherwise, the default value of type <typeparamref name="T"/>.</returns>
        public T GetConfiguration<T>(string key)
        {
            if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T)!;
        }

        /// <summary>
        /// Updates the agent's name.
        /// </summary>
        /// <param name="newName">The new name to assign to the agent.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="newName"/> is null.</exception>
        public void UpdateName(AgentName newName)
        {
            ArgumentNullException.ThrowIfNull(newName);
            Name = newName;
        }

        /// <summary>
        /// Updates the role of the agent.
        /// </summary>
        /// <param name="newRole">The new role to assign to the agent.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="newRole"/> is null.</exception>
        public void UpdateRole(AgentRole newRole)
        {
            ArgumentNullException.ThrowIfNull(newRole);
            Role = newRole;
        }

        // For reconstitution from persistence
        /// <summary>
        /// Reconstitutes an <see cref="Agent"/> instance from its persisted state.
        /// </summary>
        /// <param name="id">The unique identifier of the agent.</param>
        /// <param name="name">The name of the agent.</param>
        /// <param name="role">The role of the agent.</param>
        /// <param name="status">The current status of the agent.</param>
        /// <param name="createdAt">The timestamp when the agent was created.</param>
        /// <param name="lastActivatedAt">The timestamp of the agent's last activation, or null if never activated.</param>
        /// <param name="failureReason">The reason for the agent's failure, if applicable.</param>
        /// <param name="focusAreas">The focus areas associated with the agent.</param>
        /// <param name="capabilities">The capabilities associated with the agent.</param>
        /// <param name="configuration">The key-value configuration dictionary for the agent, or null if no configuration.</param>
        /// <returns>A reconstituted <see cref="Agent"/> instance.</returns>
        internal static Agent Reconstitute(
            AgentId id,
            AgentName name,
            AgentRole role,
            AgentStatus status,
            DateTimeOffset createdAt,
            Nullable<DateTimeOffset> lastActivatedAt,
            string failureReason,
            IEnumerable<string> focusAreas,
            IEnumerable<string> capabilities,
            IDictionary<string, object>? configuration = null)
        {
            var agent = new Agent(id, name, role)
            {
                Status = status,
                CreatedAt = createdAt,
                LastActivatedAt = lastActivatedAt,
                FailureReason = failureReason
            };
            agent._focusAreas.AddRange(focusAreas);
            agent._capabilities.AddRange(capabilities);
            if (configuration == null) return agent;
            foreach (var kv in configuration)
            {
                agent._configuration[kv.Key] = kv.Value;
            }
            return agent;
        }
    }
}