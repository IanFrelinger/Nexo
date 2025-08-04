using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Container.Tests.Commands;
using System;

namespace Nexo.Feature.Container.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Container layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class ContainerPipelineTests
{
    private readonly ILogger<ContainerPipelineTests> _logger;

    public ContainerPipelineTests()
    {
        _logger = NullLogger<ContainerPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Container_Interfaces_WorkCorrectly()
    {
        _logger.LogInformation("Starting Container interfaces test");
        
        var command = new ContainerValidationCommand(NullLogger<ContainerValidationCommand>.Instance);
        var result = command.ValidateContainerInterfaces(timeoutMs: 5000);
        
        Assert.True(result, "Container interfaces should work correctly");
        _logger.LogInformation("Container interfaces test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Container_Models_WorkCorrectly()
    {
        _logger.LogInformation("Starting Container models test");
        
        var command = new ContainerValidationCommand(NullLogger<ContainerValidationCommand>.Instance);
        var result = command.ValidateContainerModels(timeoutMs: 5000);
        
        Assert.True(result, "Container models should work correctly");
        _logger.LogInformation("Container models test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Container_UseCases_WorkCorrectly()
    {
        _logger.LogInformation("Starting Container use cases test");
        
        var command = new ContainerValidationCommand(NullLogger<ContainerValidationCommand>.Instance);
        var result = command.ValidateContainerUseCases(timeoutMs: 5000);
        
        Assert.True(result, "Container use cases should work correctly");
        _logger.LogInformation("Container use cases test completed successfully");
    }
} 