using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.CLI.Tests.Commands;
using System;

namespace Nexo.CLI.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.CLI layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class CLIPipelineTests
{
    private readonly ILogger<CLIPipelineTests> _logger;

    public CLIPipelineTests()
    {
        _logger = NullLogger<CLIPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void VersionCommand_ShouldDisplayVersionInformation()
    {
        _logger.LogInformation("Starting version command test");
        
        var command = new VersionCommandTests(NullLogger<VersionCommandTests>.Instance);
        var result = command.TestVersionCommand(timeoutMs: 5000);
        
        Assert.True(result, "Version command should display version information");
        _logger.LogInformation("Version command test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AnalyzeCommand_WithValidPath_ShouldDisplayAnalysisMessage()
    {
        _logger.LogInformation("Starting analyze command with valid path test");
        
        var command = new AnalyzeCommandTests(NullLogger<AnalyzeCommandTests>.Instance);
        var result = command.TestAnalyzeCommandWithValidPath(timeoutMs: 5000);
        
        Assert.True(result, "Analyze command with valid path should display analysis message");
        _logger.LogInformation("Analyze command with valid path test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AnalyzeCommand_WithOutputOption_ShouldUseSpecifiedOutput()
    {
        _logger.LogInformation("Starting analyze command with output option test");
        
        var command = new AnalyzeCommandTests(NullLogger<AnalyzeCommandTests>.Instance);
        var result = command.TestAnalyzeCommandWithOutputOption(timeoutMs: 5000);
        
        Assert.True(result, "Analyze command with output option should use specified output");
        _logger.LogInformation("Analyze command with output option test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AICommand_WithSuggest_ShouldDisplayAIMessage()
    {
        _logger.LogInformation("Starting AI command with suggest test");
        
        var command = new AICommandTests();
        var result = command.TestAICommandWithSuggest(timeoutMs: 5000);
        
        Assert.True(result, "AI command with suggest should display AI message");
        _logger.LogInformation("AI command with suggest test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AICommand_WithContext_ShouldDisplayContext()
    {
        _logger.LogInformation("Starting AI command with context test");
        
        var command = new AICommandTests();
        var result = command.TestAICommandWithContext(timeoutMs: 5000);
        
        Assert.True(result, "AI command with context should display context");
        _logger.LogInformation("AI command with context test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void PipelineCommand_WithExecute_ShouldDisplayPipelineMessage()
    {
        _logger.LogInformation("Starting pipeline command with execute test");
        
        var command = new PipelineCommandTests(NullLogger<PipelineCommandTests>.Instance);
        var result = command.TestPipelineCommandWithExecute(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline command with execute should display pipeline message");
        _logger.LogInformation("Pipeline command with execute test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void PipelineCommand_WithDryRun_ShouldDisplayDryRunMessage()
    {
        _logger.LogInformation("Starting pipeline command with dry run test");
        
        var command = new PipelineCommandTests(NullLogger<PipelineCommandTests>.Instance);
        var result = command.TestPipelineCommandWithDryRun(timeoutMs: 5000);
        
        Assert.True(result, "Pipeline command with dry run should display dry run message");
        _logger.LogInformation("Pipeline command with dry run test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProjectCommand_WithInit_ShouldDisplayProjectMessage()
    {
        _logger.LogInformation("Starting project command with init test");
        
        var command = new ProjectCommandTests(NullLogger<ProjectCommandTests>.Instance);
        var result = command.TestProjectCommandWithInit(timeoutMs: 5000);
        
        Assert.True(result, "Project command with init should display project message");
        _logger.LogInformation("Project command with init test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProjectCommand_WithTemplate_ShouldDisplayTemplateMessage()
    {
        _logger.LogInformation("Starting project command with template test");
        
        var command = new ProjectCommandTests(NullLogger<ProjectCommandTests>.Instance);
        var result = command.TestProjectCommandWithTemplate(timeoutMs: 5000);
        
        Assert.True(result, "Project command with template should display template message");
        _logger.LogInformation("Project command with template test completed successfully");
    }
} 