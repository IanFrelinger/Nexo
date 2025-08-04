using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Validation.Tests.Commands;
using System;

namespace Nexo.Feature.Validation.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Validation layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class ValidationPipelineTests
{
    private readonly ILogger<ValidationPipelineTests> _logger;

    public ValidationPipelineTests()
    {
        _logger = NullLogger<ValidationPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Validation_Models_WorkCorrectly()
    {
        _logger.LogInformation("Starting Validation models test");
        
        var command = new ValidationValidationCommand(NullLogger<ValidationValidationCommand>.Instance);
        var result = command.ValidateValidationModels(timeoutMs: 5000);
        
        Assert.True(result, "Validation models should work correctly");
        _logger.LogInformation("Validation models test completed successfully");
    }
} 