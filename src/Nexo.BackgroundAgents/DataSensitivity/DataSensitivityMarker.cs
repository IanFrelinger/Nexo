using System.Collections.Concurrent;

namespace Nexo.BackgroundAgents.DataSensitivity;

/// <summary>
/// Marks and tracks data sensitivity levels.
/// 
/// Provides:
/// - Marking data objects with sensitivity levels
/// - Retrieving sensitivity levels for data
/// - Checking if an agent can access specific data
/// 
/// Thread-safe implementation using concurrent collections.
/// </summary>
public interface IDataSensitivityMarker
{
    /// <summary>
    /// Get the sensitivity level for a data object.
    /// </summary>
    /// <param name="data">The data object.</param>
    /// <returns>The sensitivity level, or Public if not marked.</returns>
    IDataSensitivityLevel GetSensitivityLevel(object data);

    /// <summary>
    /// Mark a data object with a sensitivity level.
    /// </summary>
    /// <param name="data">The data object to mark.</param>
    /// <param name="level">The sensitivity level.</param>
    void MarkSensitivity(object data, IDataSensitivityLevel level);

    /// <summary>
    /// Mark a data object with a sensitivity level by name.
    /// </summary>
    /// <param name="data">The data object to mark.</param>
    /// <param name="levelName">The sensitivity level name.</param>
    /// <exception cref="ArgumentException">Thrown if level name is not found.</exception>
    void MarkSensitivity(object data, string levelName);

    /// <summary>
    /// Check if an agent with the given sensitivity level can access the data.
    /// </summary>
    /// <param name="agentLevel">The agent's maximum sensitivity level.</param>
    /// <param name="data">The data object to check.</param>
    /// <returns>True if agent can access data, false otherwise.</returns>
    bool CanAccess(IDataSensitivityLevel agentLevel, object data);
}

/// <summary>
/// Implementation of IDataSensitivityMarker.
/// </summary>
public sealed class DataSensitivityMarker : IDataSensitivityMarker
{
    private readonly IDataSensitivityRegistry _registry;
    private readonly ConcurrentDictionary<object, IDataSensitivityLevel> _markings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSensitivityMarker"/> class.
    /// </summary>
    /// <param name="registry">The sensitivity level registry.</param>
    public DataSensitivityMarker(IDataSensitivityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Get the sensitivity level for a data object.
    /// </summary>
    public IDataSensitivityLevel GetSensitivityLevel(object data)
    {
        if (data == null)
            return DataSensitivityLevels.Public;

        return _markings.TryGetValue(data, out var level)
            ? level
            : DataSensitivityLevels.Public;
    }

    /// <summary>
    /// Mark a data object with a sensitivity level.
    /// </summary>
    public void MarkSensitivity(object data, IDataSensitivityLevel level)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (level == null)
            throw new ArgumentNullException(nameof(level));

        _markings[data] = level;
    }

    /// <summary>
    /// Mark a data object with a sensitivity level by name.
    /// </summary>
    public void MarkSensitivity(object data, string levelName)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(levelName))
            throw new ArgumentException("Level name cannot be null or empty", nameof(levelName));

        var level = _registry.GetByName(levelName)
            ?? throw new ArgumentException($"Unknown sensitivity level: {levelName}", nameof(levelName));

        MarkSensitivity(data, level);
    }

    /// <summary>
    /// Check if an agent with the given sensitivity level can access the data.
    /// </summary>
    public bool CanAccess(IDataSensitivityLevel agentLevel, object data)
    {
        if (agentLevel == null)
            throw new ArgumentNullException(nameof(agentLevel));

        var dataLevel = GetSensitivityLevel(data);
        return _registry.CanAccess(agentLevel, dataLevel);
    }
}
