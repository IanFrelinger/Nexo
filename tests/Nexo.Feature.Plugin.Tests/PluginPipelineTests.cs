using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Plugin.Tests.Commands;
using System;

namespace Nexo.Feature.Plugin.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Plugin layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class PluginPipelineTests
{
    private readonly ILogger<PluginPipelineTests> _logger;

    public PluginPipelineTests()
    {
        _logger = NullLogger<PluginPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void IPlugin_Interface_WorksCorrectly()
    {
        _logger.LogInformation("Starting IPlugin interface test");
        var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
        var result = command.ValidateIPluginInterface(timeoutMs: 5000);
        Assert.True(result, "IPlugin interface should work correctly");
        _logger.LogInformation("IPlugin interface test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void IPluginManager_Interface_WorksCorrectly()
    {
        _logger.LogInformation("Starting IPluginManager interface test");
        var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
        var result = command.ValidateIPluginManagerInterface(timeoutMs: 5000);
        Assert.True(result, "IPluginManager interface should work correctly");
        _logger.LogInformation("IPluginManager interface test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void IPluginLoader_Interface_WorksCorrectly()
    {
        _logger.LogInformation("Starting IPluginLoader interface test");
        var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
        var result = command.ValidateIPluginLoaderInterface(timeoutMs: 5000);
        Assert.True(result, "IPluginLoader interface should work correctly");
        _logger.LogInformation("IPluginLoader interface test completed successfully");
    }
} 