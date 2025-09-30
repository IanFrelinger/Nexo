using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Orchestration
{
    /// <summary>
    /// Service host for Director Studio following Nexo patterns.
    /// Provides dependency injection and service composition.
    /// </summary>
    public sealed class DirectorStudioServiceHost
    {
        private readonly IServiceProvider _serviceProvider;
        
        public DirectorStudioServiceHost(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public static IServiceProvider BuildDefault(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            
            // Register Nexo orchestrator and command handlers
            services.AddSingleton<IOrchestrator, GenericCommandOrchestrator>();
            services.AddSingleton<GenericCommandOrchestrator>();
            services.AddSingleton<IPreValidator, NoopPreValidator>();
            
            // Register Director Studio commands
            services.AddTransient<IPlanGameSliceCommand, PlanGameSliceCommand>();
            services.AddTransient<IBuildWorldLayoutCommand, BuildWorldLayoutCommand>();
            services.AddTransient<IPlaceInteractionsCommand, PlaceInteractionsCommand>();
            services.AddTransient<ICreateContentBundleCommand, CreateContentBundleCommand>();
            services.AddTransient<IProposeAutoFixesCommand, ProposeAutoFixesCommand>();
            services.AddTransient<IApplyAutoFixesCommand, ApplyAutoFixesCommand>();
            
            // Register Director Studio validators
            services.AddTransient<IValidator<GamePlan>, PlayabilityValidator>();
            services.AddTransient<IValidator<GamePlan>, MechanicsValidator>();
            
            // Register genre profiles
            services.AddSingleton<GenreRegistry>();
            services.AddTransient<IGenreProfile, FPSProfile>();
            services.AddTransient<IGenreProfile, PlatformerProfile>();
            services.AddTransient<IGenreProfile, RPGProfile>();
            services.AddSingleton<GenreProfileService>();
            
            // Register offline adapters
            services.AddSingleton<IOllamaAdapter, OllamaAdapter>();
            services.AddSingleton<ITextureGenAdapter, ComfyUIAdapter>();
            services.AddSingleton<ITtsAdapter, PiperAdapter>();
            
            // Register basic services
            services.AddLogging();
            
            // Allow additional configuration
            configure?.Invoke(services);
            
            return services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Gets a service from the dependency injection container.
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }
        
        /// <summary>
        /// Gets a required service from the dependency injection container.
        /// </summary>
        public T GetRequiredService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        
        /// <summary>
        /// Runs a command through the orchestrator.
        /// </summary>
        public async ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(
            ICommand<TIn, TOut> command, TIn input, CancellationToken cancellationToken)
        {
            var orchestrator = _serviceProvider.GetRequiredService<IOrchestrator>();
            return await orchestrator.RunAsync(command, input, cancellationToken);
        }
    }
}
