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
/// Adapter for Xamarin framework code generation
/// </summary>
public partial class XamarinAdapter
{
    private readonly ILogger<XamarinAdapter> _logger;
    private readonly IAIRuntimeSelector _runtimeSelector;

    public XamarinAdapter(ILogger<XamarinAdapter> logger, IAIRuntimeSelector runtimeSelector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
    }

    public async Task<XamarinResult> GenerateXamarinCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Xamarin code for application logic");

            var result = new XamarinResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate pages
            var pages = new List<XamarinPage>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var page = await GenerateXamarinPagesAsync(controller, cancellationToken);
                if (page != null)
                {
                    pages.Add(page);
                }
            }
            result.Pages = pages;

            // Generate views
            var views = new List<XamarinView>();
            foreach (var model in applicationLogic.Models)
            {
                var view = await GenerateXamarinViewsAsync(model, cancellationToken);
                if (view != null)
                {
                    views.Add(view);
                }
            }
            result.Views = views;

            // Generate services
            var services = new List<XamarinService>();
            foreach (var service in applicationLogic.Services)
            {
                var xamarinService = await GenerateXamarinServiceAsync(service, cancellationToken);
                if (xamarinService != null)
                {
                    services.Add(xamarinService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateXamarinConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "Xamarin code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Xamarin code");
            return new XamarinResult
            {
                Success = false,
                Message = $"Xamarin code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<XamarinPage> GenerateXamarinPagesAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Xamarin page: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Xamarin,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Xamarin page for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. Xamarin page class
2. XAML markup
3. Code-behind logic
4. Navigation handling
5. Data binding
6. Event handling
7. Documentation comments

Ensure the page follows Xamarin best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Xamarin page: {ControllerName}", controller.Name);
                return null;
            }

            return new XamarinPage
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Xamarin page: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<XamarinView> GenerateXamarinViewsAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Xamarin view: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Xamarin,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Xamarin view for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. Xamarin view class
2. XAML markup
3. Code-behind logic
4. Data binding
5. Styling
6. Documentation comments

Ensure the view follows Xamarin best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Xamarin view: {ModelName}", model.Name);
                return null;
            }

            return new XamarinView
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Xamarin view: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<XamarinService> GenerateXamarinServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Xamarin service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Xamarin,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Xamarin service for the following application service:
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

Ensure the service follows Xamarin best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Xamarin service: {ServiceName}", service.Name);
                return null;
            }

            return new XamarinService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Xamarin service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<XamarinConfiguration> GenerateXamarinConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Xamarin configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Mobile,
                Framework = FrameworkType.Xamarin,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate Xamarin configuration for the following application:
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
7. Xamarin-specific configurations

Ensure the configuration follows Xamarin best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Xamarin configuration");
                return null;
            }

            return new XamarinConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Xamarin configuration");
            return null;
        }
    }
}
