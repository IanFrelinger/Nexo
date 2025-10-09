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

namespace NexoDirectorStudio.Orchestration;

/// <summary>
/// Unified Director Studio service implementation.
/// Consolidates all DirectorStudioService functionality into single source.
/// Combines the best features from all existing implementations.
/// </summary>
public sealed class DirectorStudioServiceUnified : IDirectorStudioService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHost? _host;
    private bool _disposed = false;

    /// <summary>
    /// Creates a new instance with default configuration.
    /// </summary>
    public DirectorStudioServiceUnified()
    {
        _host = CreateHost();
        _serviceProvider = _host.Services;
    }

    /// <summary>
    /// Creates a new instance with a custom service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use</param>
    public DirectorStudioServiceUnified(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _host = null;
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    public T GetService<T>() where T : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectorStudioServiceUnified));

        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not available");

        return service;
    }

    /// <summary>
    /// Checks if a service of the specified type is available.
    /// </summary>
    /// <typeparam name="T">The type of service to check</typeparam>
    /// <returns>True if the service is available, false otherwise</returns>
    public bool IsServiceAvailable<T>() where T : class
    {
        if (_disposed)
            return false;

        return _serviceProvider.GetService<T>() != null;
    }

    /// <summary>
    /// Initializes the service with default configuration.
    /// </summary>
    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectorStudioServiceUnified));

        // Service is already initialized in constructor
        // This method is provided for compatibility
    }

    /// <summary>
    /// Gets the service provider for advanced scenarios.
    /// </summary>
    /// <returns>The underlying service provider</returns>
    public object GetServiceProvider()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectorStudioServiceUnified));

        return _serviceProvider;
    }

    /// <summary>
    /// Creates a host with default service configuration.
    /// </summary>
    /// <returns>The configured host</returns>
    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    private static void ConfigureServices(IServiceCollection services)
    {
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
        
        // Register adapters
        services.AddTransient<IAutoFixAdapter, AutoFixAdapter>();
        services.AddTransient<IContentAdapter, ContentAdapter>();
        services.AddTransient<IValidationAdapter, ValidationAdapter>();
        
        // Register DTOs and models
        services.AddTransient<GamePlan>();
        services.AddTransient<WorldLayout>();
        services.AddTransient<ContentBundle>();
    }

    /// <summary>
    /// Disposes of the service and its resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _host?.Dispose();
            _disposed = true;
        }
    }
}
