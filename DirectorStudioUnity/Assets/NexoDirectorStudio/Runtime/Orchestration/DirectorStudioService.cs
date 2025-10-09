using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Application;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Orchestration
{
    /// <summary>
    /// Director Studio service implementation for Unity projects.
    /// Now uses the unified DirectorStudioServiceUnified implementation.
    /// </summary>
    public class DirectorStudioService : IDirectorStudioService
    {
        private readonly IDirectorStudioService _unifiedService;

        public DirectorStudioService()
        {
            _unifiedService = new DirectorStudioServiceUnified();
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance</returns>
        public T GetService<T>() where T : class
        {
            return _unifiedService.GetService<T>();
        }

        /// <summary>
        /// Checks if a service of the specified type is available.
        /// </summary>
        /// <typeparam name="T">The type of service to check</typeparam>
        /// <returns>True if the service is available, false otherwise</returns>
        public bool IsServiceAvailable<T>() where T : class
        {
            return _unifiedService.IsServiceAvailable<T>();
        }

        /// <summary>
        /// Initializes the service with default configuration.
        /// </summary>
        public void Initialize()
        {
            _unifiedService.Initialize();
        }

        /// <summary>
        /// Gets the service provider for advanced scenarios.
        /// </summary>
        /// <returns>The underlying service provider</returns>
        public object GetServiceProvider()
        {
            return _unifiedService.GetServiceProvider();
        }

        /// <summary>
        /// Disposes of the service and its resources.
        /// </summary>
        public void Dispose()
        {
            _unifiedService?.Dispose();
        }
    }
}