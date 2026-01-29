using System.Collections.Concurrent;

namespace Nexo.BackgroundAgents.DataSensitivity;

/// <summary>
/// Registry for managing data sensitivity levels (primitives + custom).
/// 
/// Provides:
/// - Registration of custom sensitivity levels
/// - Lookup by name (checks primitives first, then custom)
/// - Access control checking (can agent level access data level?)
/// 
/// Thread-safe implementation using concurrent collections.
/// </summary>
public interface IDataSensitivityRegistry
{
    /// <summary>
    /// Register a custom sensitivity level.
    /// </summary>
    /// <param name="level">The sensitivity level to register.</param>
    /// <exception cref="ArgumentException">Thrown if level with same name already exists.</exception>
    void Register(IDataSensitivityLevel level);

    /// <summary>
    /// Unregister a custom sensitivity level by name. Primitive levels cannot be unregistered.
    /// </summary>
    /// <param name="name">The sensitivity level name to remove.</param>
    /// <returns>True if a custom level was removed, false if not found or primitive.</returns>
    bool Unregister(string name);

    /// <summary>
    /// Get sensitivity level by name (checks both primitives and custom levels).
    /// </summary>
    /// <param name="name">The sensitivity level name.</param>
    /// <returns>The sensitivity level, or null if not found.</returns>
    IDataSensitivityLevel? GetByName(string? name);

    /// <summary>
    /// Get all registered sensitivity levels (primitives + custom), ordered by SensitivityValue.
    /// </summary>
    /// <returns>All registered sensitivity levels.</returns>
    IReadOnlyList<IDataSensitivityLevel> GetAll();

    /// <summary>
    /// Check if one level can access another (based on SensitivityValue).
    /// Agent can only access data at or below its maximum sensitivity level.
    /// </summary>
    /// <param name="agentLevel">The agent's maximum sensitivity level.</param>
    /// <param name="dataLevel">The data's sensitivity level.</param>
    /// <returns>True if agent can access data, false otherwise.</returns>
    bool CanAccess(IDataSensitivityLevel agentLevel, IDataSensitivityLevel dataLevel);
}

/// <summary>
/// Implementation of IDataSensitivityRegistry.
/// </summary>
public sealed class DataSensitivityRegistry : IDataSensitivityRegistry
{
    private readonly ConcurrentDictionary<string, IDataSensitivityLevel> _customLevels = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a custom sensitivity level.
    /// </summary>
    public void Register(IDataSensitivityLevel level)
    {
        if (level == null)
            throw new ArgumentNullException(nameof(level));

        // Check if it conflicts with a primitive
        var existingPrimitive = DataSensitivityLevels.FromName(level.Value);
        if (existingPrimitive != null)
        {
            throw new ArgumentException($"Cannot register custom level '{level.Value}' - conflicts with primitive level", nameof(level));
        }

        // Check if custom level with same name already exists
        if (_customLevels.TryGetValue(level.Value, out var existing))
        {
            throw new ArgumentException($"Custom sensitivity level '{level.Value}' is already registered", nameof(level));
        }

        _customLevels[level.Value] = level;
    }

    /// <inheritdoc />
    public bool Unregister(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (DataSensitivityLevels.FromName(name) != null)
            return false; // primitives cannot be unregistered
        return _customLevels.TryRemove(name, out _);
    }

    /// <summary>
    /// Get sensitivity level by name (checks primitives first, then custom levels).
    /// </summary>
    public IDataSensitivityLevel? GetByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Check primitives first
        var primitive = DataSensitivityLevels.FromName(name);
        if (primitive != null)
            return primitive;

        // Check custom levels
        return _customLevels.TryGetValue(name, out var custom) ? custom : null;
    }

    /// <summary>
    /// Get all registered sensitivity levels (primitives + custom), ordered by SensitivityValue.
    /// </summary>
    public IReadOnlyList<IDataSensitivityLevel> GetAll()
    {
        return DataSensitivityLevels.All
            .Concat(_customLevels.Values)
            .OrderBy(l => l.SensitivityValue)
            .ToList();
    }

    /// <summary>
    /// Check if one level can access another (based on SensitivityValue).
    /// Agent can only access data at or below its maximum sensitivity level.
    /// </summary>
    public bool CanAccess(IDataSensitivityLevel agentLevel, IDataSensitivityLevel dataLevel)
    {
        if (agentLevel == null)
            throw new ArgumentNullException(nameof(agentLevel));
        if (dataLevel == null)
            throw new ArgumentNullException(nameof(dataLevel));

        // Agent can only access data at or below its maximum sensitivity level
        return dataLevel.SensitivityValue <= agentLevel.SensitivityValue;
    }
}
