using System.Collections.Concurrent;

namespace Ashlar.BackgroundAgents.DataSensitivity;

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
