using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Template.Tests.Commands;
using System;

namespace Nexo.Feature.Template.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Template layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class TemplatePipelineTests
{
    private readonly ILogger<TemplatePipelineTests> _logger;

    public TemplatePipelineTests()
    {
        _logger = NullLogger<TemplatePipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Template_Interfaces_WorkCorrectly()
    {
        _logger.LogInformation("Starting Template interfaces test");
        
        var command = new TemplateValidationCommand(NullLogger<TemplateValidationCommand>.Instance);
        var result = command.ValidateTemplateInterfaces(timeoutMs: 5000);
        
        Assert.True(result, "Template interfaces should work correctly");
        _logger.LogInformation("Template interfaces test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Template_Models_WorkCorrectly()
    {
        _logger.LogInformation("Starting Template models test");
        
        var command = new TemplateValidationCommand(NullLogger<TemplateValidationCommand>.Instance);
        var result = command.ValidateTemplateModels(timeoutMs: 5000);
        
        Assert.True(result, "Template models should work correctly");
        _logger.LogInformation("Template models test completed successfully");
    }
} 