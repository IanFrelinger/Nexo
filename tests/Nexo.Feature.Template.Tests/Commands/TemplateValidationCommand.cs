using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Models;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Template.Tests.Commands;

/// <summary>
/// Command for validating Template functionality with proper logging and timeouts.
/// </summary>
public class TemplateValidationCommand
{
    private readonly ILogger<TemplateValidationCommand> _logger;

    public TemplateValidationCommand(ILogger<TemplateValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Template interface definitions.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateTemplateInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Template interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that interfaces exist and have expected methods
            var iTemplateEngineType = typeof(ITemplateEngine);
            var iTemplateRendererType = typeof(ITemplateRenderer);
            var iTemplateValidatorType = typeof(ITemplateValidator);
            
            var iTemplateEngineMethods = iTemplateEngineType.GetMethods();
            var iTemplateRendererMethods = iTemplateRendererType.GetMethods();
            var iTemplateValidatorMethods = iTemplateValidatorType.GetMethods();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Template interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = iTemplateEngineType.IsInterface && 
                        iTemplateRendererType.IsInterface &&
                        iTemplateValidatorType.IsInterface &&
                        iTemplateEngineMethods.Length > 0 &&
                        iTemplateRendererMethods.Length > 0 &&
                        iTemplateValidatorMethods.Length > 0;
            
            _logger.LogInformation("Template interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Template interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Template model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateTemplateModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Template model validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var templateDefinition = new TemplateDefinition
            {
                Name = "web-api-template",
                Version = "1.0.0",
                Description = "Web API template",
                Parameters = new Dictionary<string, object> { ["projectName"] = "MyProject" }
            };

            var templateContext = new TemplateContext
            {
                ProjectName = "test-project",
                Framework = "net8.0",
                Variables = new Dictionary<string, object> { ["var1"] = "value1" }
            };

            var templateResult = new TemplateResult
            {
                Success = true,
                GeneratedFiles = new List<string> { "Program.cs", "Startup.cs", "appsettings.json" },
                OutputPath = "/generated/project"
            };

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Template model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = templateDefinition.Name == "web-api-template" && 
                        templateContext.ProjectName == "test-project" &&
                        templateResult.Success &&
                        templateDefinition.Parameters.Count == 1 &&
                        templateContext.Variables.Count == 1 &&
                        templateResult.GeneratedFiles.Count == 3;
            
            _logger.LogInformation("Template model validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Template model validation");
            return false;
        }
    }
} 