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
/// Generates desktop tests
/// </summary>
public partial class DesktopTestGenerator
{
    private readonly ILogger<DesktopTestGenerator> _logger;
    private readonly IModelOrchestrator _modelOrchestrator;

    public DesktopTestGenerator(
        ILogger<DesktopTestGenerator> logger,
        IModelOrchestrator modelOrchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
    }

    public async Task<IEnumerable<DesktopTest>> GenerateTestsAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tests for desktop application");

        var tests = new List<DesktopTest>();

        foreach (var feature in applicationLogic.Features)
        {
            var featureTests = await GenerateTestsForFeatureAsync(feature, options, cancellationToken);
            tests.AddRange(featureTests);
        }

        return tests;
    }

    private async Task<IEnumerable<DesktopTest>> GenerateTestsForFeatureAsync(
        Feature feature,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating tests for feature: {FeatureName}", feature.Name);

            var prompt = $@"
Generate comprehensive tests for the following desktop feature:
Feature: {feature.Name}
Description: {feature.Description}
Requirements: {string.Join(", ", feature.Requirements)}

Target Platform: {options.TargetPlatform}
Testing Framework: {options.TestingFramework}
Architecture: {options.ArchitecturePattern}

Generate:
1. Unit tests for business logic
2. Integration tests for services
3. UI tests for components
4. View model tests
5. Data access tests
6. API tests
7. Performance tests
8. Security tests

Ensure the tests follow best practices for desktop application testing and cover all scenarios.";

            var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for tests: {FeatureName}", feature.Name);
                return new List<DesktopTest>();
            }

            return new List<DesktopTest>
            {
                new DesktopTest
                {
                    Name = $"{feature.Name}UnitTests",
                    FeatureName = feature.Name,
                    TestType = "Unit",
                    Content = ExtractUnitTests(response),
                    TestMethods = ExtractTestMethods(response, "Unit"),
                    GeneratedAt = DateTime.UtcNow
                },
                new DesktopTest
                {
                    Name = $"{feature.Name}IntegrationTests",
                    FeatureName = feature.Name,
                    TestType = "Integration",
                    Content = ExtractIntegrationTests(response),
                    TestMethods = ExtractTestMethods(response, "Integration"),
                    GeneratedAt = DateTime.UtcNow
                },
                new DesktopTest
                {
                    Name = $"{feature.Name}UITests",
                    FeatureName = feature.Name,
                    TestType = "UI",
                    Content = ExtractUITests(response),
                    TestMethods = ExtractTestMethods(response, "UI"),
                    GeneratedAt = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tests for feature: {FeatureName}", feature.Name);
            return new List<DesktopTest>();
        }
    }

    private string ExtractUnitTests(string response)
    {
        var startIndex = response.IndexOf("[Test]", StringComparison.OrdinalIgnoreCase);
        var endIndex = response.IndexOf("[TestClass]", StringComparison.OrdinalIgnoreCase);
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex);
        }
        
        return response;
    }

    private string ExtractIntegrationTests(string response)
    {
        var startIndex = response.IndexOf("Integration", StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            var endIndex = response.IndexOf("UI", StringComparison.OrdinalIgnoreCase);
            if (endIndex > startIndex)
            {
                return response.Substring(startIndex, endIndex - startIndex);
            }
            return response.Substring(startIndex);
        }
        
        return string.Empty;
    }

    private string ExtractUITests(string response)
    {
        var startIndex = response.IndexOf("UI", StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            return response.Substring(startIndex);
        }
        
        return string.Empty;
    }

    private List<string> ExtractTestMethods(string response, string testType)
    {
        var methods = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("[Test]") || line.Contains("[TestMethod]"))
            {
                methods.Add(line.Trim());
            }
        }
        
        return methods;
    }
}
