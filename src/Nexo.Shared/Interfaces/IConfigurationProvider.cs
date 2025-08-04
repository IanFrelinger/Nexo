using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Provides access to application configuration.
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets configuration of the specified type.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The configuration instance.</returns>
        Task<T> GetConfigurationAsync<T>(CancellationToken cancellationToken = default) 
            where T : class, new();

        /// <summary>
        /// Saves configuration.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="configuration">The configuration to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SaveConfigurationAsync<T>(
            T configuration,
            CancellationToken cancellationToken = default) 
            where T : class;

        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        /// <returns>The configuration storage path.</returns>
        string GetConfigurationPath();

        /// <summary>
        /// Checks if configuration exists.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if configuration exists; otherwise, false.</returns>
        Task<bool> ExistsAsync<T>(CancellationToken cancellationToken = default) 
            where T : class;

        /// <summary>
        /// Deletes configuration.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync<T>(CancellationToken cancellationToken = default) 
            where T : class;

        /// <summary>
        /// Reloads configuration from storage.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The reloaded configuration.</returns>
        Task<T> ReloadAsync<T>(CancellationToken cancellationToken = default) 
            where T : class, new();
    }
}