using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Application;

namespace NexoDirectorStudio.Orchestration
{
    /// <summary>
    /// Service bootstrap that composes Nexo orchestrator via dependency injection.
    /// This is the main entry point for Director Studio's orchestration layer.
    /// </summary>
    public class DirectorStudioService
    {
        private readonly IHost _host;
        private readonly IServiceProvider _serviceProvider;
        
        public DirectorStudioService()
        {
            _host = CreateHost();
            _serviceProvider = _host.Services;
        }
        
        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();
        }
        
        private static void ConfigureServices(IServiceCollection services)
        {
            // TODO: Register Nexo orchestrator and command handlers
            // This will be implemented in Phase 2 when we define the DTOs and commands
            
            // For now, just register basic services
            services.AddLogging();
        }
        
        /// <summary>
        /// Gets a service from the dependency injection container.
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }
        
        /// <summary>
        /// Disposes the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _host?.Dispose();
        }
    }
}
