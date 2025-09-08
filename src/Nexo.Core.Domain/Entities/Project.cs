using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Domain.Entities
{
/// <summary>
/// Represents a project, encapsulating its unique identity, properties, status, associated agents,
/// and lifecycle management functionalities.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Stores a collection of agents associated with the project.
    /// This list is used to manage and track agents linked to the project.
    /// </summary>
    private readonly List<Agent> _agents = new List<Agent>();

    /// <summary>
    /// Stores metadata associated with the project as key-value pairs.
    /// </summary>
    private readonly Dictionary<string, object> _metadata = new Dictionary<string, object>();

    /// <summary>
    /// Gets the unique identifier of the project.
    /// </summary>
    public ProjectId Id { get; private set; }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public ProjectName Name { get; private set; }

    /// <summary>
    /// Gets or sets the project file system path, represented as a normalized and validated value object.
    /// </summary>
    public ProjectPath Path { get; private set; }

    /// <summary>
    /// Gets the container runtime assigned to the project, determining the environment in which the project operates.
    /// </summary>
    public ContainerRuntime Runtime { get; private set; }

    /// <summary>
    /// Gets a read-only list of agents associated with the project.
    /// </summary>
    public IReadOnlyList<Agent> Agents => _agents.AsReadOnly();

    /// <summary>
    /// Gets the current lifecycle status of the project.
    /// </summary>
    public ProjectStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the project was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the project was last modified.
    /// </summary>
    public Nullable<DateTimeOffset> ModifiedAt { get; private set; }

    /// <summary>
    /// Gets the metadata associated with the project as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => new ReadOnlyDictionary<string, object>(_metadata);

    /// <summary>
    /// Represents a project entity with properties like ID, name, path, runtime, status, and timestamps.
    /// </summary>
    public Project(ProjectName name, ProjectPath path, ContainerRuntime runtime)
    {
            ArgumentNullException.ThrowIfNull(name);
            if (path is not null)
            {
                ArgumentNullException.ThrowIfNull(runtime);

                Id = ProjectId.New();
                Name = name;
                Path = path;
                Runtime = runtime;
                Status = ProjectStatus.NotInitialized;
                CreatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                throw new ArgumentNullException(nameof(path));
            }
    }

    /// <summary>
    /// Adds an agent to the project.
    /// </summary>
    /// <param name="agent">The agent to be added to the project.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided agent is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the agent already exists in the project.</exception>
    public void AddAgent(Agent agent)
    {
            ArgumentNullException.ThrowIfNull(agent);

            if (_agents.Any(a => a.Id == agent.Id))
                throw new InvalidOperationException($"Agent with ID '{agent.Id}' already exists in the project.");

            _agents.Add(agent);
            UpdateModifiedTime();
    }

    /// <summary>
    /// Removes an agent from the project.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to be removed.</param>
    /// <returns>Returns true if the agent was successfully removed; otherwise, false.</returns>
    public bool RemoveAgent(AgentId agentId)
    {
        if (agentId == null) throw new ArgumentNullException(nameof(agentId));

        var removed = _agents.RemoveAll(a => a.Id == agentId) > 0;
        if (removed)
        {
            UpdateModifiedTime();
        }
        
        return removed;
    }

    /// <summary>
    /// Retrieves an agent by its ID from the project.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to retrieve.</param>
    /// <returns>The agent associated with the specified ID if found; otherwise, null.</returns>
    public Agent? GetAgent(AgentId agentId) =>
        _agents.FirstOrDefault(a => a.Id == agentId);

    /// <summary>
    /// Initializes the project, transitioning its status from <see cref="ProjectStatus.NotInitialized"/>
    /// to <see cref="ProjectStatus.Initialized"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project is not in the <see cref="ProjectStatus.NotInitialized"/> state.
    /// </exception>
    public void Initialize()
    {
        if (Status != ProjectStatus.NotInitialized)
            throw new InvalidOperationException($"Cannot initialize project in '{Status}' status. Project must be in 'NotInitialized' status.");
            
        Status = ProjectStatus.Initialized;
        UpdateModifiedTime();
    }

    /// <summary>
    /// Starts the project, transitioning its status to <see cref="ProjectStatus.Running"/> if the current state allows it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project is not in a valid state to be started.
    /// The project must have a status of either <see cref="ProjectStatus.Initialized"/> or <see cref="ProjectStatus.Stopped"/>.
    /// </exception>
    public void Start()
    {
        if (Status != ProjectStatus.Initialized && Status != ProjectStatus.Stopped)
            throw new InvalidOperationException($"Cannot start project in '{Status}' status. Project must be in 'Initialized' or 'Stopped' status.");
            
        Status = ProjectStatus.Running;
        UpdateModifiedTime();
    }

    /// <summary>
    /// Stops the project.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project is not in the 'Running' status.
    /// </exception>
    public void Stop()
    {
        if (Status != ProjectStatus.Running)
            throw new InvalidOperationException(
                $"Cannot stop project in '{Status}' status. Project must be in 'Running' status.");
            
        Status = ProjectStatus.Stopped;
        UpdateModifiedTime();
    }

    /// <summary>
    /// Marks the project as failed.
    /// </summary>
    /// <param name="reason">The reason for the project's failure.</param>
    /// <exception cref="ArgumentNullException">Thrown when the reason is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the reason is empty or whitespace.</exception>
    public void MarkAsFailed(string reason)
    {
        Status = ProjectStatus.Failed;
        SetMetadata("FailureReason", reason);
        SetMetadata("FailureTime", DateTimeOffset.UtcNow);
        UpdateModifiedTime();
    }

    /// <summary>
    /// Updates the project name.
    /// </summary>
    /// <param name="newName">The new project name.</param>
    /// <exception cref="ArgumentNullException">Thrown when the new name is null.</exception>
    public void UpdateName(ProjectName newName)
    {
        if (newName == null) throw new ArgumentNullException(nameof(newName));

        if (Name != newName)
        {
            Name = newName;
            UpdateModifiedTime();
        }
    }

    /// <summary>
    /// Sets a metadata value for the project.
    /// </summary>
    /// <param name="key">The key of the metadata to set.</param>
    /// <param name="value">The value to associate with the specified metadata key.</param>
    /// <exception cref="ArgumentException">Thrown when the key is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public void SetMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _metadata[key] = value;
        UpdateModifiedTime();
    }

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    /// <typeparam name="T">The expected type of the metadata value.</typeparam>
    /// <param name="key">The key associated with the metadata.</param>
    /// <returns>The metadata value if it exists and matches the expected type; otherwise, the default value of type T.</returns>
    public T GetMetadata<T>(string key)
    {
        if (_metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default(T)!;
    }

    /// <summary>
    /// Updates the last modified timestamp of the project to the current UTC date and time.
    /// </summary>
    private void UpdateModifiedTime()
    {
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    // For reconstitution from persistence
    /// <summary>
    /// Reconstitutes a <see cref="Project"/> instance from persisted data.
    /// </summary>
    /// <param name="id">The unique identifier of the project.</param>
    /// <param name="name">The name of the project.</param>
    /// <param name="path">The file path associated with the project.</param>
    /// <param name="runtime">The container runtime used by the project.</param>
    /// <param name="status">The current status of the project.</param>
    /// <param name="createdAt">The timestamp when the project was created.</param>
    /// <param name="modifiedAt">The timestamp of the last modification, if any, applied to the project.</param>
    /// <param name="agents">The collection of agents associated with the project.</param>
    /// <param name="metadata">Optional metadata associated with the project as key-value pairs.</param>
    /// <returns>A reconstituted <see cref="Project"/> instance populated with the provided data.</returns>
    internal static Project Reconstitute(
        ProjectId id,
        ProjectName name,
        ProjectPath path,
        ContainerRuntime runtime,
        ProjectStatus status,
        DateTimeOffset createdAt,
        Nullable<DateTimeOffset> modifiedAt,
        IEnumerable<Agent> agents,
        IDictionary<string, object>? metadata = null)
    {
        var project = new Project(name, path, runtime)
        {
            Id = id,
            Status = status,
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };

        foreach (var agent in agents)
        {
            project._agents.Add(agent);
        }

        if (metadata != null)
        {
            foreach (var kv in metadata)
            {
                project._metadata[kv.Key] = kv.Value;
            }
        }

        return project;
    }
}
}