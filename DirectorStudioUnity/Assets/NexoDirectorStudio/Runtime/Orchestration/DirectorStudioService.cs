using System;
using System.Collections.Generic;
using System.Linq;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Orchestration
{
    /// <summary>
    /// Main service composition and dependency injection container for Director Studio.
    /// Provides access to all commands, validators, and adapters.
    /// </summary>
    public class DirectorStudioService : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new();
        private bool _disposed = false;

        public DirectorStudioService()
        {
            InitializeServices();
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance</returns>
        public T GetService<T>() where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectorStudioService));

            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            // Create service on demand if not found
            var newService = CreateService<T>();
            if (newService != null)
            {
                _services[typeof(T)] = newService;
                return newService;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not available");
        }

        private T CreateService<T>() where T : class
        {
            // Create command services
            if (typeof(T) == typeof(IPlanGameSliceCommand))
                return new PlanGameSliceCommand() as T;
            
            if (typeof(T) == typeof(IBuildWorldLayoutCommand))
                return new BuildWorldLayoutCommand() as T;
            
            if (typeof(T) == typeof(IPlaceInteractionsCommand))
                return new PlaceInteractionsCommand() as T;
            
            if (typeof(T) == typeof(ICreateContentBundleCommand))
                return new CreateContentBundleCommand() as T;
            
            if (typeof(T) == typeof(IProposeAutoFixesCommand))
                return new ProposeAutoFixesCommand() as T;
            
            if (typeof(T) == typeof(IApplyAutoFixesCommand))
                return new ApplyAutoFixesCommand() as T;

            // Create validator services
            if (typeof(T) == typeof(IEnumerable<IValidator<GamePlan>>))
            {
                var validators = new List<IValidator<GamePlan>>
                {
                    (IValidator<GamePlan>)new PlayabilityValidator(),
                    (IValidator<GamePlan>)new MechanicsValidator(),
                    (IValidator<GamePlan>)new PerformanceValidator()
                };
                return validators as T;
            }
            
            if (typeof(T) == typeof(IValidator<ContentBundle>))
                return new ContentBundleValidator() as T;

            // Create adapter services
            if (typeof(T) == typeof(IOllamaAdapter))
                return new OllamaAdapter() as T;
            
            if (typeof(T) == typeof(ITextureGenAdapter))
                return new ComfyUIAdapter() as T;
            
            if (typeof(T) == typeof(ITtsAdapter))
                return new PiperAdapter() as T;

            // Create profile services
            if (typeof(T) == typeof(GenreProfileService))
                return new GenreProfileService() as T;

            return null;
        }

        private void InitializeServices()
        {
            // Initialize core services
            _services[typeof(GenreProfileService)] = new GenreProfileService();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose all disposable services
                foreach (var service in _services.Values.OfType<IDisposable>())
                {
                    service?.Dispose();
                }
                
                _services.Clear();
                _disposed = true;
            }
        }
    }
}
