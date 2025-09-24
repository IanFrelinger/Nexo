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
/// Adapter for Web API framework code generation
/// </summary>
public class WebApiAdapter
{
    private readonly ILogger<WebApiAdapter> _logger;
    private readonly IAIRuntimeSelector _runtimeSelector;

    public WebApiAdapter(ILogger<WebApiAdapter> logger, IAIRuntimeSelector runtimeSelector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
    }

    public async Task<WebApiResult> GenerateWebApiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Web API code for application logic");

            var result = new WebApiResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate controllers
            var controllers = new List<WebApiController>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var webApiController = await GenerateWebApiControllerAsync(controller, cancellationToken);
                if (webApiController != null)
                {
                    controllers.Add(webApiController);
                }
            }
            result.Controllers = controllers;

            // Generate models
            var models = new List<WebApiModel>();
            foreach (var model in applicationLogic.Models)
            {
                var webApiModel = await GenerateWebApiModelAsync(model, cancellationToken);
                if (webApiModel != null)
                {
                    models.Add(webApiModel);
                }
            }
            result.Models = models;

            // Generate services
            var services = new List<WebApiService>();
            foreach (var service in applicationLogic.Services)
            {
                var webApiService = await GenerateWebApiServiceAsync(service, cancellationToken);
                if (webApiService != null)
                {
                    services.Add(webApiService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateWebApiConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "Web API code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web API code");
            return new WebApiResult
            {
                Success = false,
                Message = $"Web API code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<WebApiController> GenerateWebApiControllerAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Web API controller: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.WebApi,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Web API controller for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. Controller class with proper attributes
2. HTTP action methods (GET, POST, PUT, DELETE)
3. Model binding and validation
4. Error handling
5. Logging
6. Documentation comments
7. Swagger attributes

Ensure the controller follows Web API best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Web API controller: {ControllerName}", controller.Name);
                return null;
            }

            return new WebApiController
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web API controller: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<WebApiModel> GenerateWebApiModelAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Web API model: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.WebApi,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Web API model for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. Model class with properties
2. Data annotations for validation
3. JSON serialization attributes
4. Documentation comments
5. Swagger attributes
6. DTOs if needed

Ensure the model follows Web API best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Web API model: {ModelName}", model.Name);
                return null;
            }

            return new WebApiModel
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web API model: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<WebApiService> GenerateWebApiServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Web API service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.WebApi,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Web API service for the following application service:
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

Ensure the service follows Web API best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Web API service: {ServiceName}", service.Name);
                return null;
            }

            return new WebApiService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web API service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<WebApiConfiguration> GenerateWebApiConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Web API configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Web,
                Framework = FrameworkType.WebApi,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate Web API configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Models: {string.Join(", ", applicationLogic.Models.Select(m => m.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Swagger/OpenAPI configuration
4. CORS configuration
5. Authentication/Authorization setup
6. Logging configuration
7. Error handling middleware

Ensure the configuration follows Web API best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Web API configuration");
                return null;
            }

            return new WebApiConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web API configuration");
            return null;
        }
    }
}
