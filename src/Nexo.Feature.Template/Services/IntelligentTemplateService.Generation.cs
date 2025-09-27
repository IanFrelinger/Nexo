using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Template generation functionality
    /// </summary>
    public partial class IntelligentTemplateService
    {
        public async Task<string> GenerateProjectStructureAsync(string projectType, IDictionary<string, object> requirements, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating project structure for type: {ProjectType}", projectType);

            try
            {
                if (string.IsNullOrEmpty(projectType))
                {
                    throw new ArgumentException("Project type cannot be null or empty", nameof(projectType));
                }

                var prompt = CreateProjectStructurePrompt(projectType, requirements);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 4000,
                    Temperature = 0.3
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("project structure generation", ModelType.TextGeneration, cancellationToken);
                if (provider == null)
                    throw new InvalidOperationException("No suitable model provider available");
                var availableModels = await provider.GetAvailableModelsAsync(cancellationToken);
                var modelInfo = availableModels.FirstOrDefault(m => m.ModelType == ModelType.TextGeneration);
                if (modelInfo == null)
                    throw new InvalidOperationException("No suitable model available");
                var model = await provider.LoadModelAsync(modelInfo.Name, cancellationToken);
                var response = await model.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project structure");
                throw;
            }
        }

        public async Task<string> GenerateConfigurationTemplateAsync(string configurationType, IDictionary<string, object> settings, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating configuration template for type: {ConfigurationType}", configurationType);

            try
            {
                if (string.IsNullOrEmpty(configurationType))
                {
                    throw new ArgumentException("Configuration type cannot be null or empty", nameof(configurationType));
                }

                var prompt = CreateConfigurationTemplatePrompt(configurationType, settings);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2500,
                    Temperature = 0.3
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("configuration template generation", ModelType.TextGeneration, cancellationToken);
                if (provider == null)
                    throw new InvalidOperationException("No suitable model provider available");
                var availableModels = await provider.GetAvailableModelsAsync(cancellationToken);
                var modelInfo = availableModels.FirstOrDefault(m => m.ModelType == ModelType.TextGeneration);
                if (modelInfo == null)
                    throw new InvalidOperationException("No suitable model available");
                var model = await provider.LoadModelAsync(modelInfo.Name, cancellationToken);
                var response = await model.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating configuration template");
                throw;
            }
        }

        public async Task<string> GenerateDocumentationTemplateAsync(string documentationType, IDictionary<string, object> context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating documentation template for type: {DocumentationType}", documentationType);

            try
            {
                if (string.IsNullOrEmpty(documentationType))
                {
                    throw new ArgumentException("Documentation type cannot be null or empty", nameof(documentationType));
                }

                var prompt = CreateDocumentationTemplatePrompt(documentationType, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 3000,
                    Temperature = 0.3
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("documentation template generation", ModelType.TextGeneration, cancellationToken);
                if (provider == null)
                    throw new InvalidOperationException("No suitable model provider available");
                var availableModels = await provider.GetAvailableModelsAsync(cancellationToken);
                var modelInfo = availableModels.FirstOrDefault(m => m.ModelType == ModelType.TextGeneration);
                if (modelInfo == null)
                    throw new InvalidOperationException("No suitable model available");
                var model = await provider.LoadModelAsync(modelInfo.Name, cancellationToken);
                var response = await model.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating documentation template");
                throw;
            }
        }
    }
}
