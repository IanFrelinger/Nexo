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
    /// Template improvement functionality
    /// </summary>
    public partial class IntelligentTemplateService
    {
        public async Task<IList<string>> SuggestTemplateImprovementsAsync(string template, IDictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting template improvement suggestions");

            try
            {
                if (string.IsNullOrEmpty(template))
                {
                    return new List<string> { "No template provided for improvement suggestions" };
                }

                var prompt = CreateTemplateImprovementPrompt(template, context ?? new Dictionary<string, object>());
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.3
                };

                // Get the best provider for the task
                var provider = await _modelOrchestrator.GetBestModelForTaskAsync("template improvement", ModelType.TextGeneration, cancellationToken);
                if (provider == null)
                    throw new InvalidOperationException("No suitable model provider available");
                var availableModels = await provider.GetAvailableModelsAsync(cancellationToken);
                var modelInfo = availableModels.FirstOrDefault(m => m.ModelType == ModelType.TextGeneration);
                if (modelInfo == null)
                    throw new InvalidOperationException("No suitable model available");
                var model = await provider.LoadModelAsync(modelInfo.Name, cancellationToken);
                var response = await model.ProcessAsync(request, cancellationToken);
                return ParseSuggestions(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template improvement suggestions");
                return new List<string> { $"Error during template improvement: {ex.Message}" };
            }
        }

        private IList<string> ParseSuggestions(string aiResponse)
        {
            var suggestions = new List<string>();
            
            if (string.IsNullOrEmpty(aiResponse))
            {
                return suggestions;
            }

            // Split by numbered lines or bullet points
            var lines = aiResponse.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && 
                    (trimmedLine.StartsWith("1.") || 
                     trimmedLine.StartsWith("2.") || 
                     trimmedLine.StartsWith("3.") || 
                     trimmedLine.StartsWith("4.") || 
                     trimmedLine.StartsWith("5.") || 
                     trimmedLine.StartsWith("6.") || 
                     trimmedLine.StartsWith("7.") || 
                     trimmedLine.StartsWith("8.") || 
                     trimmedLine.StartsWith("9.") || 
                     trimmedLine.StartsWith("10.") ||
                     trimmedLine.StartsWith("-") ||
                     trimmedLine.StartsWith("•")))
                {
                    // Remove the number/bullet and clean up
                    var suggestion = trimmedLine;
                    if (suggestion.Contains("."))
                    {
                        suggestion = suggestion.Substring(suggestion.IndexOf(".") + 1).Trim();
                    }
                    else if (suggestion.StartsWith("-") || suggestion.StartsWith("•"))
                    {
                        suggestion = suggestion.Substring(1).Trim();
                    }
                    
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        suggestions.Add(suggestion);
                    }
                }
            }

            return suggestions;
        }
    }
}
