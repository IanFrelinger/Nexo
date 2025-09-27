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
/// Generates platform-specific code and build configurations
/// </summary>
public partial class DesktopPlatformGenerator
{
    private readonly ILogger<DesktopPlatformGenerator> _logger;
    private readonly IModelOrchestrator _modelOrchestrator;

    public DesktopPlatformGenerator(
        ILogger<DesktopPlatformGenerator> logger,
        IModelOrchestrator modelOrchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
    }

    public async Task<IEnumerable<PlatformSpecificCode>> GeneratePlatformSpecificAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating platform-specific code for desktop application");

        var platformCodes = new List<PlatformSpecificCode>();

        var platforms = new[] { "Windows", "macOS", "Linux" };
        
        foreach (var platform in platforms)
        {
            var platformCode = await GeneratePlatformCodeAsync(platform, applicationLogic, options, cancellationToken);
            if (platformCode != null)
            {
                platformCodes.Add(platformCode);
            }
        }

        return platformCodes;
    }

    public async Task<DesktopBuildConfiguration> GenerateBuildConfigurationAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating build configuration for desktop application");

        try
        {
            var prompt = $@"
Generate build configuration for desktop application:
Application: {applicationLogic.ApplicationName}
Target Platform: {options.TargetPlatform}
Framework: {options.Framework}
Architecture: {options.ArchitecturePattern}

Generate:
1. Project file (.csproj)
2. Solution file (.sln)
3. Build scripts for each platform
4. Package configuration
5. Dependencies management
6. Build targets
7. Deployment scripts

Ensure the build configuration follows best practices for desktop applications.";

            var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for build configuration");
                return null;
            }

            return new DesktopBuildConfiguration
            {
                ProjectFile = await GenerateProjectFileAsync(applicationLogic, options, cancellationToken),
                SolutionFile = await GenerateSolutionFileAsync(applicationLogic, options, cancellationToken),
                BuildScripts = await GenerateBuildScriptsAsync(applicationLogic, options, cancellationToken),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating build configuration");
            return null;
        }
    }

    private async Task<PlatformSpecificCode> GeneratePlatformCodeAsync(
        string platform,
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating platform-specific code for: {Platform}", platform);

            var prompt = $@"
Generate platform-specific code for {platform} desktop application:
Application: {applicationLogic.ApplicationName}
Target Platform: {platform}
Framework: {options.Framework}
Architecture: {options.ArchitecturePattern}

Generate:
1. Platform-specific UI adaptations
2. Native API integrations
3. Platform-specific services
4. File system handling
5. Registry/Preferences handling
6. Platform-specific configurations
7. Native library bindings

Ensure the code follows {platform} best practices and conventions.";

            var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for platform: {Platform}", platform);
                return null;
            }

            return new PlatformSpecificCode
            {
                Platform = platform,
                Content = response,
                NativeAPIs = ExtractNativeAPIs(response),
                PlatformServices = ExtractPlatformServices(response),
                Configurations = ExtractConfigurations(response),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating platform-specific code for: {Platform}", platform);
            return null;
        }
    }

    private async Task<DesktopProjectFile> GenerateProjectFileAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $@"
Generate project file (.csproj) for desktop application:
Application: {applicationLogic.ApplicationName}
Target Platform: {options.TargetPlatform}
Framework: {options.Framework}
Features: {string.Join(", ", applicationLogic.Features.Select(f => f.Name))}

Generate:
1. Target framework
2. Package references
3. Project references
4. Build configurations
5. Platform targets
6. Output settings
7. Dependencies";

        var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
        
        return new DesktopProjectFile
        {
            Content = response,
            TargetFramework = options.Framework,
            Platform = options.TargetPlatform,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DesktopSolutionFile> GenerateSolutionFileAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $@"
Generate solution file (.sln) for desktop application:
Application: {applicationLogic.ApplicationName}
Target Platform: {options.TargetPlatform}
Architecture: {options.ArchitecturePattern}

Generate:
1. Solution structure
2. Project references
3. Build configurations
4. Platform targets
5. Solution folders
6. Dependencies
7. Build order";

        var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
        
        return new DesktopSolutionFile
        {
            Content = response,
            Projects = ExtractProjects(response),
            Configurations = ExtractBuildConfigurations(response),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<IEnumerable<DesktopBuildScript>> GenerateBuildScriptsAsync(
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var buildScripts = new List<DesktopBuildScript>();
        var platforms = new[] { "Windows", "macOS", "Linux" };

        foreach (var platform in platforms)
        {
            var script = await GenerateBuildScriptForPlatformAsync(platform, applicationLogic, options, cancellationToken);
            if (script != null)
            {
                buildScripts.Add(script);
            }
        }

        return buildScripts;
    }

    private async Task<DesktopBuildScript> GenerateBuildScriptForPlatformAsync(
        string platform,
        ApplicationLogic applicationLogic,
        DesktopGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $@"
Generate build script for {platform} desktop application:
Application: {applicationLogic.ApplicationName}
Target Platform: {platform}
Framework: {options.Framework}

Generate:
1. Build commands
2. Dependencies installation
3. Compilation steps
4. Testing commands
5. Packaging commands
6. Deployment steps
7. Platform-specific optimizations";

        var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
        
        return new DesktopBuildScript
        {
            Platform = platform,
            Content = response,
            Commands = ExtractBuildCommands(response),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private List<string> ExtractNativeAPIs(string response)
    {
        var apis = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("DllImport") || line.Contains("extern"))
            {
                apis.Add(line.Trim());
            }
        }
        
        return apis;
    }

    private List<string> ExtractPlatformServices(string response)
    {
        var services = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("class") && line.Contains("Service"))
            {
                services.Add(line.Trim());
            }
        }
        
        return services;
    }

    private List<string> ExtractConfigurations(string response)
    {
        var configurations = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("Configuration") || line.Contains("Settings"))
            {
                configurations.Add(line.Trim());
            }
        }
        
        return configurations;
    }

    private List<string> ExtractProjects(string response)
    {
        var projects = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("Project(") || line.Contains(".csproj"))
            {
                projects.Add(line.Trim());
            }
        }
        
        return projects;
    }

    private List<string> ExtractBuildConfigurations(string response)
    {
        var configurations = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("Debug") || line.Contains("Release"))
            {
                configurations.Add(line.Trim());
            }
        }
        
        return configurations;
    }

    private List<string> ExtractBuildCommands(string response)
    {
        var commands = new List<string>();
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("dotnet") || line.Contains("msbuild") || line.Contains("nuget"))
            {
                commands.Add(line.Trim());
            }
        }
        
        return commands;
    }
}
