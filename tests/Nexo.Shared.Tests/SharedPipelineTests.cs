using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Shared.Tests.Commands;
using Nexo.Core.Application.Enums;
using System;

namespace Nexo.Shared.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Shared layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class SharedPipelineTests
{
    private readonly ILogger<SharedPipelineTests> _logger;

    public SharedPipelineTests()
    {
        _logger = NullLogger<SharedPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void BuildConfiguration_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting BuildConfiguration test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateBuildConfiguration(timeoutMs: 5000);
        
        Assert.True(result, "BuildConfiguration properties should work correctly");
        _logger.LogInformation("BuildConfiguration test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void BuildResult_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting BuildResult test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateBuildResult(timeoutMs: 5000);
        
        Assert.True(result, "BuildResult properties should work correctly");
        _logger.LogInformation("BuildResult test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void CommandResult_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting CommandResult test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateCommandResult(timeoutMs: 5000);
        
        Assert.True(result, "CommandResult properties should work correctly");
        _logger.LogInformation("CommandResult test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ValidationResult_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting ValidationResult test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateValidationResult(timeoutMs: 5000);
        
        Assert.True(result, "ValidationResult properties should work correctly");
        _logger.LogInformation("ValidationResult test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AgentRequestType_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting AgentRequestType enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateAgentRequestType(timeoutMs: 5000);
        
        Assert.True(result, "AgentRequestType enum values should be defined");
        _logger.LogInformation("AgentRequestType enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AnalysisStatus_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting AnalysisStatus enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateAnalysisStatus(timeoutMs: 5000);
        
        Assert.True(result, "AnalysisStatus enum values should be defined");
        _logger.LogInformation("AnalysisStatus enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void FeaturePriority_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting FeaturePriority enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateFeaturePriority(timeoutMs: 5000);
        
        Assert.True(result, "FeaturePriority enum values should be defined");
        _logger.LogInformation("FeaturePriority enum test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void Enums_Values_AreDefined()
    {
        _logger.LogInformation("Starting enum values test");
        
        // Test that enum values are accessible
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TaskAssignment));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.CodeReview));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.ArchitectureDesign));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TestCreation));
        
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Success));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.PartialSuccess));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Failed));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Cancelled));
        
        Assert.True(Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Low));
        Assert.True(Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Medium));
        Assert.True(Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.High));
        Assert.True(Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Critical));
        
        _logger.LogInformation("Enum values test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void InterfaceImplementations_AreAccessible()
    {
        _logger.LogInformation("Starting interface implementations test");
        
        // Test that interfaces can be referenced (compilation test)
        Assert.NotNull(typeof(Nexo.Shared.Interfaces.ICommandExecutor));
        Assert.NotNull(typeof(Nexo.Shared.Interfaces.ICommandValidator));
        
        _logger.LogInformation("Interface implementations test completed successfully");
    }
} 