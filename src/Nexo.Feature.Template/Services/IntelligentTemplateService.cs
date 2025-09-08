using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using System.Linq;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Intelligent template service that provides AI-powered template generation and adaptation.
    /// </summary>
    public class IntelligentTemplateService : IIntelligentTemplateService
    {
        private readonly ILogger<IntelligentTemplateService> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ITemplateService _baseTemplateService;

        public IntelligentTemplateService(
            ILogger<IntelligentTemplateService> logger,
            IModelOrchestrator modelOrchestrator,
            ITemplateService baseTemplateService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _baseTemplateService = baseTemplateService ?? throw new ArgumentNullException(nameof(baseTemplateService));
        }

        public async Task<string> GenerateTemplateAsync(string description, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating intelligent template for description: {Description}", description);

            try
            {
                var prompt = CreateTemplateGenerationPrompt(description, parameters);
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

        // ITemplateService implementation
        public async Task<string> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.GetTemplateAsync(templateName, cancellationToken);
        }

        public async Task SaveTemplateAsync(string templateName, string content, CancellationToken cancellationToken = default)
        {
            await _baseTemplateService.SaveTemplateAsync(templateName, content, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.GetAvailableTemplatesAsync(cancellationToken);
        }

        public async Task DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            await _baseTemplateService.DeleteTemplateAsync(templateName, cancellationToken);
        }

        public async Task<bool> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.ValidateTemplateAsync(templateName, cancellationToken);
        }

        public async Task<IList<string>> SuggestTemplateImprovementsAsync(string template, IDictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting template improvement suggestions");

            try
            {
                if (string.IsNullOrEmpty(template))
                {
                    return new List<string> { "No template provided for improvement suggestions" };
                }

                var prompt = CreateTemplateImprovementPrompt(template, context);
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

        private string CreateTemplateGenerationPrompt(string description, IDictionary<string, object> parameters)
        {
            var parametersInfo = parameters != null ? $"Parameters: {string.Join(", ", parameters.Values)}" : "";
            
            return $@"Generate a comprehensive template based on the following description:

Description: {description}
{parametersInfo}

Please create a template that includes:
1. Proper file structure and organization
2. Best practices and conventions
3. Error handling and validation
4. Documentation and comments
5. Configuration management
6. Testing considerations
7. Security considerations
8. Performance optimizations
9. Maintainability features
10. Extensibility patterns

The template should be:
- Production-ready
- Follow industry standards
- Include comprehensive documentation
- Be easily customizable
- Support multiple environments
- Include proper error handling
- Follow SOLID principles

Format the response as complete, compilable code with proper structure and organization.";
        }

        private string CreateTemplateAdaptationPrompt(string template, IDictionary<string, object> requirements)
        {
            var requirementsInfo = string.Join("\n", requirements.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
            
            return $@"Adapt the following template based on the specified requirements:

Original Template:
{template}

Requirements:
{requirementsInfo}

Please adapt the template to:
1. Meet all specified requirements
2. Maintain the original structure and quality
3. Add any missing functionality
4. Update configuration as needed
5. Ensure compatibility with requirements
6. Preserve best practices
7. Add necessary documentation
8. Include required dependencies
9. Update error handling
10. Optimize for the specific use case

The adapted template should:
- Fulfill all requirements
- Maintain code quality
- Be production-ready
- Include proper documentation
- Follow best practices

Format the response as the complete adapted template.";
        }

        private string CreateTemplateImprovementPrompt(string template, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following template and provide improvement suggestions:

Template:
{template}

{contextInfo}

Please provide improvement suggestions for:
1. Code quality and best practices
2. Performance optimizations
3. Security enhancements
4. Maintainability improvements
5. Error handling
6. Documentation
7. Testing coverage
8. Configuration management
9. Dependency management
10. Architectural improvements

For each suggestion, provide:
- The specific improvement
- The reasoning behind it
- Expected benefits
- Implementation guidance

Format your response as a numbered list of specific, actionable improvements.";
        }

        private string CreateProjectStructurePrompt(string projectType, IDictionary<string, object> requirements)
        {
            var requirementsInfo = requirements != null ? $"Requirements: {string.Join(", ", requirements.Values)}" : "";
            
            return $@"Generate a complete project structure for a {projectType} project:

{requirementsInfo}

Please create a project structure that includes:
1. Directory organization
2. File naming conventions
3. Project file structure
4. Configuration files
5. Documentation structure
6. Test organization
7. Build configuration
8. Deployment configuration
9. CI/CD configuration
10. Development tools configuration

The structure should:
- Follow industry best practices
- Be scalable and maintainable
- Support team collaboration
- Include proper separation of concerns
- Support multiple environments
- Include proper documentation
- Follow naming conventions
- Support testing strategies

Format the response as a complete directory structure with file contents and explanations.";
        }

        private string CreateConfigurationTemplatePrompt(string configurationType, IDictionary<string, object> settings)
        {
            var settingsInfo = settings != null ? $"Settings: {string.Join(", ", settings.Values)}" : "";
            
            return $@"Generate a configuration template for {configurationType}:

{settingsInfo}

Please create a configuration template that includes:
1. Environment-specific settings
2. Security configurations
3. Performance settings
4. Logging configuration
5. Database configuration
6. External service configuration
7. Feature flags
8. Monitoring configuration
9. Error handling settings
10. Development tools configuration

The configuration should:
- Be environment-aware
- Include proper validation
- Support secure defaults
- Be easily maintainable
- Include documentation
- Support different deployment scenarios
- Follow configuration best practices

Format the response as a complete configuration template with proper structure and documentation.";
        }

        private string CreateDocumentationTemplatePrompt(string documentationType, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Generate a documentation template for {documentationType}:

{contextInfo}

Please create a documentation template that includes:
1. Overview and purpose
2. Installation instructions
3. Configuration guide
4. Usage examples
5. API documentation
6. Troubleshooting guide
7. Performance considerations
8. Security considerations
9. Deployment guide
10. Contributing guidelines

The documentation should:
- Be comprehensive and clear
- Include code examples
- Be easily navigable
- Include troubleshooting
- Follow documentation best practices
- Be maintainable
- Include proper formatting
- Support multiple audiences

Format the response as a complete documentation template with proper structure and content.";
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