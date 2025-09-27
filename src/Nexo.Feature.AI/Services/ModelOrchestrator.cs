using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Orchestrates AI model operations and provider management.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class ModelOrchestrator : IModelOrchestrator
{
    private readonly List<IModelProvider> _providers;
    private readonly ILogger<ModelOrchestrator> _logger;

    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _providers = new List<IModelProvider>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a request using the best available model.
    /// </summary>
    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// Registers a new model provider.
    /// </summary>
    public Task RegisterProviderAsync(IModelProvider provider, CancellationToken cancellationToken = default)
    {
        return RegisterProviderInternalAsync(provider, cancellationToken);
    }

    /// <summary>
    /// Gets the best model provider for a specific task.
    /// </summary>
    public Task<IModelProvider?> GetBestModelForTaskAsync(string task, Enums.ModelType modelType, CancellationToken cancellationToken = default)
    {
        return GetBestModelForTaskInternalAsync(task, modelType, cancellationToken);
    }

    /// <summary>
    /// Process a request using the orchestrator.
    /// </summary>
    public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        // ProcessAsync is an alias for ExecuteAsync
        return await ExecuteAsync(request, cancellationToken);
    }
}