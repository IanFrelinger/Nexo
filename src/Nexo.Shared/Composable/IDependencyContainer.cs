using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Interface for dependency injection container
    /// </summary>
    public interface IDependencyContainer
    {
        /// <summary>
        /// Registers a service with the container
        /// </summary>
        Task RegisterAsync<TInterface, TImplementation>() where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a service instance with the container
        /// </summary>
        Task RegisterInstanceAsync<TInterface>(TInterface instance);
        
        /// <summary>
        /// Registers a factory function with the container
        /// </summary>
        Task RegisterFactoryAsync<TInterface>(Func<TInterface> factory);
        
        /// <summary>
        /// Resolves a service from the container
        /// </summary>
        Task<TInterface> ResolveAsync<TInterface>();
        
        /// <summary>
        /// Resolves a service by type from the container
        /// </summary>
        Task<object> ResolveAsync(Type serviceType);
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        Task<bool> IsRegisteredAsync<TInterface>();
        
        /// <summary>
        /// Gets all registered service types
        /// </summary>
        Task<IReadOnlyList<Type>> GetRegisteredTypesAsync();
        
        /// <summary>
        /// Clears all registrations
        /// </summary>
        Task ClearAsync();
    }
}
