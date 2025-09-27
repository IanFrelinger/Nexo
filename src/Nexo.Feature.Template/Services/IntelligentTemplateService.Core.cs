using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Core intelligent template functionality
    /// </summary>
    public partial class IntelligentTemplateService
    {
        public async Task<string> GenerateTemplateAsync(string description, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating intelligent template for description: {Description}", description);

            try
            {
                var prompt = CreateTemplateGenerationPrompt(description, parameters ?? new Dictionary<string, object>());
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2500,
                    Temperature = 0.4
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("template generation", ModelType.TextGeneration, cancellationToken);
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
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions to allow proper cancellation handling
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating intelligent template");
                return $"Error generating template: {ex.Message}";
            }
        }

        public async Task<string> AdaptTemplateAsync(string templateName, IDictionary<string, object> adaptations, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adapting template: {TemplateName}", templateName);

            try
            {
                var originalTemplate = await _baseTemplateService.GetTemplateAsync(templateName, cancellationToken);
                var prompt = CreateTemplateAdaptationPrompt(originalTemplate, adaptations);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 3000,
                    Temperature = 0.3
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("template adaptation", ModelType.TextGeneration, cancellationToken);
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
                _logger.LogError(ex, "Error adapting template: {TemplateName}", templateName);
                return $"Error adapting template: {ex.Message}";
            }
        }
    }
}
