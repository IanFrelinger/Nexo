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
/// Adapter for Blazor framework code generation
/// </summary>
public class BlazorAdapter
{
    private readonly ILogger<BlazorAdapter> _logger;
    private readonly IAIRuntimeSelector _runtimeSelector;

    public BlazorAdapter(ILogger<BlazorAdapter> logger, IAIRuntimeSelector runtimeSelector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
    }

    public async Task<BlazorServerResult> GenerateBlazorServerCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Blazor Server code for application logic");

            var result = new BlazorServerResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate components
            var components = new List<BlazorComponent>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var component = await GenerateBlazorComponentsAsync(controller, cancellationToken);
                if (component != null)
                {
                    components.Add(component);
                }
            }
            result.Components = components;

            // Generate pages
            var pages = new List<BlazorPage>();
            foreach (var model in applicationLogic.Models)
            {
                var page = await GenerateBlazorPagesAsync(model, cancellationToken);
                if (page != null)
                {
                    pages.Add(page);
                }
            }
            result.Pages = pages;

            // Generate services
            var services = new List<BlazorService>();
            foreach (var service in applicationLogic.Services)
            {
                var blazorService = await GenerateBlazorServiceAsync(service, cancellationToken);
                if (blazorService != null)
                {
                    services.Add(blazorService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateBlazorConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "Blazor Server code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor Server code");
            return new BlazorServerResult
            {
                Success = false,
                Message = $"Blazor Server code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<BlazorWebAssemblyResult> GenerateBlazorWebAssemblyCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Blazor WebAssembly code for application logic");

            var result = new BlazorWebAssemblyResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate components
            var components = new List<BlazorComponent>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var component = await GenerateBlazorComponentsAsync(controller, cancellationToken);
                if (component != null)
                {
                    components.Add(component);
                }
            }
            result.Components = components;

            // Generate pages
            var pages = new List<BlazorPage>();
            foreach (var model in applicationLogic.Models)
            {
                var page = await GenerateBlazorPagesAsync(model, cancellationToken);
                if (page != null)
                {
                    pages.Add(page);
                }
            }
            result.Pages = pages;

            // Generate services
            var services = new List<BlazorService>();
            foreach (var service in applicationLogic.Services)
            {
                var blazorService = await GenerateBlazorServiceAsync(service, cancellationToken);
                if (blazorService != null)
                {
                    services.Add(blazorService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateBlazorConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "Blazor WebAssembly code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor WebAssembly code");
            return new BlazorWebAssemblyResult
            {
                Success = false,
                Message = $"Blazor WebAssembly code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<BlazorComponent> GenerateBlazorComponentsAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Blazor component: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.Blazor,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Blazor component for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. Blazor component with @page directive
2. HTML markup with Razor syntax
3. C# code-behind logic
4. Event handling
5. Data binding
6. Styling
7. Documentation comments

Ensure the component follows Blazor best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Blazor component: {ControllerName}", controller.Name);
                return null;
            }

            return new BlazorComponent
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor component: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<BlazorPage> GenerateBlazorPagesAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Blazor page: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.Blazor,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Blazor page for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. Blazor page with @page directive
2. HTML markup with Razor syntax
3. Model binding
4. Form handling
5. Validation
6. Styling
7. Documentation comments

Ensure the page follows Blazor best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Blazor page: {ModelName}", model.Name);
                return null;
            }

            return new BlazorPage
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor page: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<BlazorService> GenerateBlazorServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Blazor service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.Blazor,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Blazor service for the following application service:
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

Ensure the service follows Blazor best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Blazor service: {ServiceName}", service.Name);
                return null;
            }

            return new BlazorService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<BlazorConfiguration> GenerateBlazorConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Blazor configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.Blazor,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate Blazor configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Models: {string.Join(", ", applicationLogic.Models.Select(m => m.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Routing configuration
4. Authentication/Authorization setup
5. Logging configuration
6. Error handling middleware
7. Blazor-specific configurations

Ensure the configuration follows Blazor best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Blazor configuration");
                return null;
            }

            return new BlazorConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Blazor configuration");
            return null;
        }
    }
}
