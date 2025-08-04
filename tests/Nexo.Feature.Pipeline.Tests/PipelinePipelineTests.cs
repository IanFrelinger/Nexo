using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Pipeline.Tests.Commands;
using Nexo.Feature.Pipeline.Enums;
using System;

namespace Nexo.Feature.Pipeline.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Pipeline layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class PipelinePipelineTests
{
    private readonly ILogger<PipelinePipelineTests> _logger;

    public PipelinePipelineTests()
    {
        _logger = NullLogger<PipelinePipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Pipeline_Interfaces_WorkCorrectly()
    {
        _logger.LogInformation("Starting Pipeline interfaces test");
        
        var command = new PipelineValidationCommand(NullLogger<PipelineValidationCommand>.Instance);
        var result = command.ValidatePipelineInterfaces(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline interfaces should work correctly");
        _logger.LogInformation("Pipeline interfaces test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Pipeline_Models_WorkCorrectly()
    {
        _logger.LogInformation("Starting Pipeline models test");
        
        var command = new PipelineValidationCommand(NullLogger<PipelineValidationCommand>.Instance);
        var result = command.ValidatePipelineModels(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline models should work correctly");
        _logger.LogInformation("Pipeline models test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Pipeline_Enums_WorkCorrectly()
    {
        _logger.LogInformation("Starting Pipeline enums test");
        
        var command = new PipelineValidationCommand(NullLogger<PipelineValidationCommand>.Instance);
        var result = command.ValidatePipelineEnums(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline enums should work correctly");
        _logger.LogInformation("Pipeline enums test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Pipeline_ExecutionContext_WorksCorrectly()
    {
        _logger.LogInformation("Starting Pipeline execution context test");
        
        var command = new PipelineValidationCommand(NullLogger<PipelineValidationCommand>.Instance);
        var result = command.ValidatePipelineExecutionContext(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline execution context should work correctly");
        _logger.LogInformation("Pipeline execution context test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void CommandCategory_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting CommandCategory enum validation");
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.FileSystem));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Container));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Analysis));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Project));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.CLI));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Template));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Validation));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Agent));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Plugin));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Platform));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Logging));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Configuration));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Network));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Database));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Security));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Testing));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Build));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Deployment));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Monitoring));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Utility));
        Assert.True(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Custom));
        _logger.LogInformation("CommandCategory enum validation completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AggregatorExecutionStrategy_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting AggregatorExecutionStrategy enum validation");
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Sequential));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Parallel));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Conditional));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Phased));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.DependencyOrdered));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.ResourceAware));
        Assert.True(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Custom));
        _logger.LogInformation("AggregatorExecutionStrategy enum validation completed successfully");
    }
} 