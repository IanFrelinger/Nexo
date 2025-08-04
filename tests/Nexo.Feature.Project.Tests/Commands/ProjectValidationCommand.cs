using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Application.UseCases.InitializeProject;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Project.Tests.Commands;

/// <summary>
/// Command for validating Project functionality with proper logging and timeouts.
/// </summary>
public class ProjectValidationCommand
{
    private readonly ILogger<ProjectValidationCommand> _logger;

    public ProjectValidationCommand(ILogger<ProjectValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Project interface definitions.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Project interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that interfaces exist and have expected methods
            var iProjectRepositoryType = typeof(IProjectRepository);
            var iProjectInitializerType = typeof(IProjectInitializer);
            var iInitializeProjectUseCaseType = typeof(IInitializeProjectUseCase);
            
            var iProjectRepositoryMethods = iProjectRepositoryType.GetMethods();
            var iProjectInitializerMethods = iProjectInitializerType.GetMethods();
            var iInitializeProjectUseCaseMethods = iInitializeProjectUseCaseType.GetMethods();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = iProjectRepositoryType.IsInterface && 
                        iProjectInitializerType.IsInterface &&
                        iInitializeProjectUseCaseType.IsInterface &&
                        iProjectRepositoryMethods.Length > 0 &&
                        iProjectInitializerMethods.Length > 0 &&
                        iInitializeProjectUseCaseMethods.Length > 0;
            
            _logger.LogInformation("Project interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Project interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Project model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Project model validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var initializeProjectRequest = new InitializeProjectRequest(
                name: "test-project",
                path: "/test/path",
                runtime: "net8.0",
                force: true,
                agentIds: new List<string> { "agent1" },
                metadata: new Dictionary<string, object> { ["option1"] = "value1" }
            );
            var solutionScaffoldingRequest = new SolutionScaffoldingRequest
            {
                SolutionName = "test-solution",
                OutputPath = "/test/solution",
                TemplateName = "web-api",
                Configuration = new SolutionScaffoldingConfiguration
                {
                    Projects = new List<ProjectConfiguration>(),
                    Structure = new SolutionStructureConfiguration
                    {
                        CustomFolderMappings = new Dictionary<string, string>(),
                        SolutionFolders = new List<SolutionFolderConfiguration>()
                    }
                },
                TemplateParameters = new Dictionary<string, object> { ["param1"] = "value1" }
            };
            var solutionScaffoldingResult = new SolutionScaffoldingResult
            {
                ScaffoldingId = "id-1",
                SolutionName = "test-solution",
                SolutionPath = "/test/solution",
                TemplateName = "web-api",
                Status = ScaffoldingStatus.Success,
                Projects = new List<ScaffoldedProject>(),
                GeneratedFiles = new List<GeneratedFile> { new GeneratedFile { Name = "Program.cs", FilePath = "/test/solution/Program.cs", Template = "default", FileSize = 100, Errors = new List<string>() } },
                ScaffoldingStartTime = DateTime.UtcNow,
                ScaffoldingEndTime = DateTime.UtcNow,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = initializeProjectRequest.Name == "test-project" &&
                        solutionScaffoldingRequest.SolutionName == "test-solution" &&
                        solutionScaffoldingResult.IsSuccessful &&
                        (initializeProjectRequest.Metadata?.Count ?? 0) == 1 &&
                        solutionScaffoldingResult.GeneratedFiles.Count == 1;
            _logger.LogInformation("Project model validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Project model validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Project use case functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectUseCases(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Project use case validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that use case classes exist and can be instantiated
            var initializeProjectUseCaseType = typeof(InitializeProjectUseCase);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project use case validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = initializeProjectUseCaseType.IsClass && 
                        !initializeProjectUseCaseType.IsAbstract;
            
            _logger.LogInformation("Project use case validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Project use case validation");
            return false;
        }
    }
} 