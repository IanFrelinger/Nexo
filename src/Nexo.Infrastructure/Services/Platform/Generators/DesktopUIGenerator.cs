using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Platform.Generators;

/// <summary>
/// Generates desktop UI components and view models
/// </summary>
public partial class DesktopUIGenerator
{
    private readonly ILogger<DesktopUIGenerator> _logger;
    private readonly IModelOrchestrator _modelOrchestrator;

    public DesktopUIGenerator(
        ILogger<DesktopUIGenerator> logger,
        IModelOrchestrator modelOrchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
    }

    public async Task<IEnumerable<DesktopUIComponent>> GenerateUIComponentsAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating UI components for desktop application");

        var components = new List<DesktopUIComponent>();

        foreach (var feature in applicationLogic.Features)
        {
            var component = await GenerateUIComponentForFeatureAsync(feature, options, cancellationToken);
            if (component != null)
            {
                components.Add(component);
            }
        }

        return components;
    }

    public async Task<IEnumerable<DesktopViewModel>> GenerateViewModelsAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating view models for desktop application");

        var viewModels = new List<DesktopViewModel>();

        foreach (var feature in applicationLogic.Features)
        {
            var viewModel = await GenerateViewModelForFeatureAsync(feature, options, cancellationToken);
            if (viewModel != null)
            {
                viewModels.Add(viewModel);
            }
        }

        return viewModels;
    }

    private async Task<DesktopUIComponent> GenerateUIComponentForFeatureAsync(
        Feature feature,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating UI component for feature: {FeatureName}", feature.Name);

            var prompt = $@"
Generate a desktop UI component for the following feature:
Feature: {feature.Name}
Description: {feature.Description}
Requirements: {string.Join(", ", feature.Requirements)}

Target Platform: {options.TargetPlatform}
UI Framework: {options.UIFramework}
Design Pattern: {options.DesignPattern}

Generate a complete UI component with:
1. XAML markup for the user interface
2. Code-behind with event handlers
3. Data binding setup
4. Styling and layout
5. Accessibility features
6. Responsive design considerations

Ensure the component follows desktop application best practices and is optimized for the target platform.";

            var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for UI component: {FeatureName}", feature.Name);
                return null;
            }

            return new DesktopUIComponent
            {
                Name = $"{feature.Name}Component",
                FeatureName = feature.Name,
                XamlContent = ExtractXamlContent(response),
                CodeBehindContent = ExtractCodeBehindContent(response),
                Styles = ExtractStyles(response),
                DataContext = $"{feature.Name}ViewModel",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating UI component for feature: {FeatureName}", feature.Name);
            return null;
        }
    }

    private async Task<DesktopViewModel> GenerateViewModelForFeatureAsync(
        Feature feature,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating view model for feature: {FeatureName}", feature.Name);

            var prompt = $@"
Generate a desktop view model for the following feature:
Feature: {feature.Name}
Description: {feature.Description}
Requirements: {string.Join(", ", feature.Requirements)}

Target Platform: {options.TargetPlatform}
UI Framework: {options.UIFramework}
Design Pattern: {options.DesignPattern}

Generate a complete view model with:
1. Properties for data binding
2. Commands for user interactions
3. Observable collections for lists
4. Validation logic
5. Business logic methods
6. Event handling
7. Dependency injection setup

Ensure the view model follows MVVM pattern and desktop application best practices.";

            var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for view model: {FeatureName}", feature.Name);
                return null;
            }

            return new DesktopViewModel
            {
                Name = $"{feature.Name}ViewModel",
                FeatureName = feature.Name,
                CodeContent = response,
                Properties = ExtractProperties(response),
                Commands = ExtractCommands(response),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating view model for feature: {FeatureName}", feature.Name);
            return null;
        }
    }

    private string ExtractXamlContent(string response)
    {
        // Extract XAML content from the response
        var startIndex = response.IndexOf("<UserControl", StringComparison.OrdinalIgnoreCase);
        var endIndex = response.LastIndexOf("</UserControl>", StringComparison.OrdinalIgnoreCase);
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + "</UserControl>".Length);
        }
        
        return response;
    }

    private string ExtractCodeBehindContent(string response)
    {
        // Extract code-behind content from the response
        var startIndex = response.IndexOf("public partial class", StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            return response.Substring(startIndex);
        }
        
        return response;
    }

    private string ExtractStyles(string response)
    {
        // Extract styles from the response
        var startIndex = response.IndexOf("<Style", StringComparison.OrdinalIgnoreCase);
        var endIndex = response.LastIndexOf("</Style>", StringComparison.OrdinalIgnoreCase);
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + "</Style>".Length);
        }
        
        return string.Empty;
    }

    private List<string> ExtractProperties(string response)
    {
        var properties = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("public") && (line.Contains("get;") || line.Contains("set;")))
            {
                properties.Add(line.Trim());
            }
        }
        
        return properties;
    }

    private List<string> ExtractCommands(string response)
    {
        var commands = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("ICommand") || line.Contains("RelayCommand"))
            {
                commands.Add(line.Trim());
            }
        }
        
        return commands;
    }
}
