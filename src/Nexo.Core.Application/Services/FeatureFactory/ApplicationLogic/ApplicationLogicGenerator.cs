using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic.Generators;

namespace Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic
{
    /// <summary>
    /// Orchestrator for application logic generation that delegates to specialized generators.
    /// </summary>
    public partial class ApplicationLogicGenerator : IApplicationLogicGenerator
    {
        private readonly ILogger<ApplicationLogicGenerator> _logger;
        private readonly ControllerGenerator _controllerGenerator;
        private readonly ServiceGenerator _serviceGenerator;
        private readonly ModelGenerator _modelGenerator;
        private readonly ViewGenerator _viewGenerator;
        private readonly ConfigurationGenerator _configurationGenerator;

        public ApplicationLogicGenerator(ILogger<ApplicationLogicGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _controllerGenerator = new ControllerGenerator(logger);
            _serviceGenerator = new ServiceGenerator(logger);
            _modelGenerator = new ModelGenerator(logger);
            _viewGenerator = new ViewGenerator(logger);
            _configurationGenerator = new ConfigurationGenerator(logger);
        }

        public async Task<ApplicationLogicResult> GenerateApplicationLogicAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating complete application logic for {EntityCount} entities", domainLogic.Entities.Count);

                var result = new ApplicationLogicResult
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow
                };

                // Generate controllers
                var controllers = await _controllerGenerator.GenerateControllersAsync(domainLogic.Entities, cancellationToken);
                result.Controllers.AddRange(controllers);

                // Generate services
                var services = await _serviceGenerator.GenerateServicesAsync(domainLogic.DomainServices, cancellationToken);
                result.Services.AddRange(services);

                // Generate models
                var models = await _modelGenerator.GenerateModelsAsync(domainLogic.Entities, cancellationToken);
                result.Models.AddRange(models);

                // Generate views
                var views = await _viewGenerator.GenerateViewsAsync(domainLogic.Entities, cancellationToken);
                result.Views.AddRange(views);

                // Generate configuration
                var configurations = await _configurationGenerator.GenerateConfigurationAsync(domainLogic, cancellationToken);
                result.Configurations.AddRange(configurations);

                result.Success = true;
                result.Message = "Application logic generated successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application logic");
                return new ApplicationLogicResult
                {
                    Success = false,
                    Message = $"Application logic generation failed: {ex.Message}",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }
    }
}
