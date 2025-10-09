using System;

namespace NexoDirectorStudio.Orchestration;

/// <summary>
/// Unified interface for Director Studio service operations.
/// Consolidates all DirectorStudioService functionality into a single contract.
/// </summary>
public interface IDirectorStudioService : IDisposable
{
    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    T GetService<T>() where T : class;

    /// <summary>
    /// Checks if a service of the specified type is available.
    /// </summary>
    /// <typeparam name="T">The type of service to check</typeparam>
    /// <returns>True if the service is available, false otherwise</returns>
    bool IsServiceAvailable<T>() where T : class;

    /// <summary>
    /// Initializes the service with default configuration.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets the service provider for advanced scenarios.
    /// </summary>
    /// <returns>The underlying service provider</returns>
    object GetServiceProvider();
}
