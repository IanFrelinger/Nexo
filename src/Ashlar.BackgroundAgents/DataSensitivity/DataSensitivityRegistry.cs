using System.Collections.Concurrent;

namespace Ashlar.BackgroundAgents.DataSensitivity;

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
