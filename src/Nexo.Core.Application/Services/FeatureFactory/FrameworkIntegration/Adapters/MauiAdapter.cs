using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration.Adapters;

/// <summary>
/// Adapter for MAUI framework code generation
/// </summary>
public class MauiAdapter
{
    private readonly ILogger<MauiAdapter> _logger;
    private readonly IAIRuntimeSelector _runtimeSelector;

    public MauiAdapter(ILogger<MauiAdapter> logger, IAIRuntimeSelector runtimeSelector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
    }

    public async Task<MauiResult> GenerateMauiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating MAUI code for application logic");

            var result = new MauiResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate pages
            var pages = new List<MauiPage>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var page = await GenerateMauiPagesAsync(controller, cancellationToken);
                if (page != null)
                {
                    pages.Add(page);
                }
            }
            result.Pages = pages;

            // Generate views
            var views = new List<MauiView>();
            foreach (var model in applicationLogic.Models)
            {
                var view = await GenerateMauiViewsAsync(model, cancellationToken);
                if (view != null)
                {
                    views.Add(view);
                }
            }
            result.Views = views;

            // Generate services
            var services = new List<MauiService>();
            foreach (var service in applicationLogic.Services)
            {
                var mauiService = await GenerateMauiServiceAsync(service, cancellationToken);
                if (mauiService != null)
                {
                    services.Add(mauiService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateMauiConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "MAUI code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MAUI code");
            return new MauiResult
            {
                Success = false,
                Message = $"MAUI code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<MauiPage> GenerateMauiPagesAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating MAUI page: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Maui,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a MAUI page for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. MAUI page class
2. XAML markup
3. Code-behind logic
4. Navigation handling
5. Data binding
6. Event handling
7. Documentation comments

Ensure the page follows MAUI best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for MAUI page: {ControllerName}", controller.Name);
                return null;
            }

            return new MauiPage
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MAUI page: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<MauiView> GenerateMauiViewsAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating MAUI view: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Maui,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a MAUI view for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. MAUI view class
2. XAML markup
3. Code-behind logic
4. Data binding
5. Styling
6. Documentation comments

Ensure the view follows MAUI best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for MAUI view: {ModelName}", model.Name);
                return null;
            }

            return new MauiView
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MAUI view: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<MauiService> GenerateMauiServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating MAUI service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Maui,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a MAUI service for the following application service:
Name: {service.Name}
Description: {service.Description}
Methods: {string.Join(", ", service.Methods.Select(m => m.Name))}

Generate:
1. Service interface
2. Service implementation
3. Dependency injection setup
4. Business logic methods
5. Error handling
6. Logging
7. Documentation comments

Ensure the service follows MAUI best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for MAUI service: {ServiceName}", service.Name);
                return null;
            }

            return new MauiService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MAUI service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<MauiConfiguration> GenerateMauiConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating MAUI configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Maui,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate MAUI configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Models: {string.Join(", ", applicationLogic.Models.Select(m => m.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Navigation configuration
4. Authentication/Authorization setup
5. Logging configuration
6. Error handling middleware
7. MAUI-specific configurations

Ensure the configuration follows MAUI best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for MAUI configuration");
                return null;
            }

            return new MauiConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MAUI configuration");
            return null;
        }
    }
}
