using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Default implementation of dependency injection container
    /// </summary>
    public class DependencyContainer : IDependencyContainer
    {
        private readonly Dictionary<Type, ServiceRegistration> _registrations;
        
        public DependencyContainer()
        {
            _registrations = new Dictionary<Type, ServiceRegistration>();
        }
        
        public async Task RegisterAsync<TInterface, TImplementation>() where TImplementation : class, TInterface
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            
            _registrations[interfaceType] = new ServiceRegistration
            {
                InterfaceType = interfaceType,
                ImplementationType = implementationType,
                RegistrationType = ServiceRegistrationType.Implementation
            };
            
            await Task.CompletedTask;
        }
        
        public async Task RegisterInstanceAsync<TInterface>(TInterface instance)
        {
            var interfaceType = typeof(TInterface);
            
            _registrations[interfaceType] = new ServiceRegistration
            {
                InterfaceType = interfaceType,
                Instance = instance,
                RegistrationType = ServiceRegistrationType.Instance
            };
            
            await Task.CompletedTask;
        }
        
        public async Task RegisterFactoryAsync<TInterface>(Func<TInterface> factory)
        {
            var interfaceType = typeof(TInterface);
            
            _registrations[interfaceType] = new ServiceRegistration
            {
                InterfaceType = interfaceType,
                Factory = () => factory(),
                RegistrationType = ServiceRegistrationType.Factory
            };
            
            await Task.CompletedTask;
        }
        
        public async Task<TInterface> ResolveAsync<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            var result = await ResolveAsync(interfaceType);
            return (TInterface)result;
        }
        
        public async Task<object> ResolveAsync(Type serviceType)
        {
            if (!_registrations.TryGetValue(serviceType, out var registration))
            {
                throw new InvalidOperationException($"Service {serviceType.Name} is not registered");
            }
            
            switch (registration.RegistrationType)
            {
                case ServiceRegistrationType.Instance:
                    return registration.Instance;
                    
                case ServiceRegistrationType.Factory:
                    return registration.Factory();
                    
                case ServiceRegistrationType.Implementation:
                    return Activator.CreateInstance(registration.ImplementationType);
                    
                default:
                    throw new InvalidOperationException($"Unknown registration type: {registration.RegistrationType}");
            }
        }
        
        public async Task<bool> IsRegisteredAsync<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return _registrations.ContainsKey(interfaceType);
        }
        
        public async Task<IReadOnlyList<Type>> GetRegisteredTypesAsync()
        {
            return _registrations.Keys.ToList();
        }
        
        public async Task ClearAsync()
        {
            _registrations.Clear();
        }
    }
    
    /// <summary>
    /// Service registration information
    /// </summary>
    internal class ServiceRegistration
    {
        public Type InterfaceType { get; set; }
        public Type ImplementationType { get; set; }
        public object Instance { get; set; }
        public Func<object> Factory { get; set; }
        public ServiceRegistrationType RegistrationType { get; set; }
    }
    
    /// <summary>
    /// Service registration types
    /// </summary>
    internal enum ServiceRegistrationType
    {
        Implementation,
        Instance,
        Factory
    }
}
