using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.CodeGeneration;
using Ashlar.Orchestration.Agents.Learning;
using Ashlar.Orchestration.Build.Models;
using Ashlar.Orchestration.Communication.Models;
using Ashlar.Orchestration.Configuration;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>Tests for orchestration models coverage.</summary>
public class OrchestrationModelsCoverageTests
{
    [Fact]
    public void Build_models_round_trip()
    {
        var err = new BuildError { Code = "E1", Message = "fail", File = "f.cs", Line = 1, Severity = BuildErrorSeverity.Fatal };
        err.Severity.Should().Be(BuildErrorSeverity.Fatal);

        var warn = new BuildWarning { Code = "W1", Message = "warn", File = "f.cs" };
        var metrics = new BuildMetrics
        {
            ArtifactSizeBytes = 100,
            ScriptCount = 1,
            AssetCount = 2,
            ShaderCount = 3,
            CompileTime = TimeSpan.FromSeconds(1),
            AssetProcessingTime = TimeSpan.FromSeconds(2),
        };

        var output = new BuildOutput
        {
            BuildId = "b1",
            Success = false,
            Target = BuildTarget.WebGL,
            ArtifactPath = "/out",
            Duration = TimeSpan.FromMinutes(1),
            Errors = new[] { err },
            Warnings = new[] { warn },
            Metrics = metrics,
        };
        output.Errors.Should().ContainSingle();
        output.Metrics!.ShaderCount.Should().Be(3);

        var cfg = new BuildConfiguration
        {
            ProjectPath = "/proj",
            Target = BuildTarget.Linux,
            Development = false,
            ScriptDebugging = false,
            OutputPath = "/out",
            Scenes = new[] { "Main" },
            Defines = new Dictionary<string, string> { ["DEBUG"] = "1" },
        };
        cfg.Defines.Should().ContainKey("DEBUG");
    }

    [Fact]
    public void Communication_messages_expose_all_properties()
    {
        var payload = JsonDocument.Parse("""{"k":1}""").RootElement;
        var output = new OutputEmitted
        {
            MessageId = "m1",
            FromAgentId = "a1",
            ToAgentId = "a2",
            MessageType = "OutputEmitted",
            Output = new { x = 1 },
            OutputSchema = "schema",
            Payload = payload,
        };
        output.MessageType.Should().Be("OutputEmitted");
        output.Output.Should().NotBeNull();

        var dep = new DependencyResolved
        {
            MessageId = "m2",
            FromAgentId = "a1",
            MessageType = "DependencyResolved",
            DependencyAgentId = "dep",
            DependencyOutput = "out",
        };
        dep.MessageType.Should().Be("DependencyResolved");

        var state = new AgentStateChanged
        {
            MessageId = "m3",
            FromAgentId = "a1",
            MessageType = "AgentStateChanged",
            NewState = "Ready",
            PreviousState = "Pending",
        };
        state.MessageType.Should().Be("AgentStateChanged");

        var error = new AgentError
        {
            MessageId = "m4",
            FromAgentId = "a1",
            MessageType = "AgentError",
            ErrorMessage = "boom",
            ErrorType = "Test",
            StackTrace = "trace",
        };
        error.MessageType.Should().Be("AgentError");

        var req = new DataRequest
        {
            MessageId = "m5",
            FromAgentId = "a1",
            MessageType = "DataRequest",
            RequestedDataType = "metrics",
            Filter = payload,
        };
        req.MessageType.Should().Be("DataRequest");

        var resp = new DataResponse
        {
            MessageId = "m6",
            FromAgentId = "a2",
            MessageType = "DataResponse",
            RequestMessageId = "m5",
            Data = new { ok = true },
        };
        resp.MessageType.Should().Be("DataResponse");

        var fwd = new ForwardedAgentMessage
        {
            MessageId = "m7",
            FromAgentId = "remote",
            MessageType = "ForwardedAgentMessage",
            SourceNodeId = "node-1",
        };
        fwd.MessageType.Should().Be("ForwardedAgentMessage");
    }

    [Fact]
    public async Task CodeAnalyzer_detects_issues_and_complexity()
    {
        var analyzer = new CodeAnalyzer(NullLogger<CodeAnalyzer>.Instance);
        var code = """
            if (true) { password = "secret"; eval(x); }
            for (var i = 0; i < 10; i++) { while (true) { } }
            """;

        var analysis = await analyzer.AnalyzeAsync(code, "csharp");
        analysis.Language.Should().Be("csharp");
        analysis.LineCount.Should().BeGreaterThan(1);
        analysis.Complexity.Should().BeGreaterThan(1);
        analysis.Issues.Should().NotBeEmpty();
        analysis.Issues.Should().Contain(i => i.Severity == IssueSeverity.High);

        var issue = new CodeIssue
        {
            Category = "security",
            Severity = IssueSeverity.Critical,
            Message = "test",
            LineNumber = 42,
        };
        issue.LineNumber.Should().Be(42);
    }

    [Fact]
    public void ResourceUsage_record_round_trips()
    {
        var usage = new ResourceUsage
        {
            AgentId = "a1",
            Duration = TimeSpan.FromSeconds(5),
            EstimatedComputeSeconds = 10,
            RequiredContextTokens = 8000,
            RequiredMemoryMB = 256,
        };
        usage.AgentId.Should().Be("a1");
        usage.RequiredMemoryMB.Should().Be(256);
    }

    [Fact]
    public void AgentMemory_records_executions_feedback_and_patterns()
    {
        var memory = new AgentMemory(NullLogger<AgentMemory>.Instance);
        var record = new ExecutionRecord
        {
            AgentId = "a1",
            ExecutedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2),
            Success = false,
            ErrorMessage = "failed",
        };
        memory.RecordExecution("a1", record);
        memory.RecordFeedback("a1", new FeedbackRecord
        {
            AgentId = "a1",
            ProvidedAt = DateTimeOffset.UtcNow,
            Rating = 2,
            Comment = "bad",
        });

        memory.GetExecutionHistory("a1").Should().ContainSingle();
        memory.GetFeedbackHistory("a1").Should().ContainSingle();
        var patterns = memory.GetLearnedPatterns("a1");
        patterns.AgentId.Should().Be("a1");
        patterns.TotalExecutions.Should().Be(1);
        patterns.CommonIssues.Should().Contain("failed");
    }

    [Fact]
    public void Asset_ports_and_validation_models_round_trip()
    {
        var req = new ImageGenerationRequest
        {
            Prompt = "icon",
            NegativePrompt = "blur",
            Size = ImageSize.Portrait768x1024,
            Style = "pixel",
            Seed = 7,
            GuidanceScale = 1.5,
        };
        req.Size.Should().Be(ImageSize.Portrait768x1024);

        var img = new GeneratedImage
        {
            FilePath = "/tmp/x.png",
            Size = ImageSize.Square512,
            MimeType = "image/png",
            Seed = 1,
            Metadata = new Dictionary<string, object> { ["k"] = 1 },
        };
        img.Metadata.Should().ContainKey("k");

        var prompt = new GenerationPrompt
        {
            TextPrompt = "hero",
            NegativePrompt = "noise",
            StyleReference = "anime",
            Parameters = new Dictionary<string, object> { ["size"] = "1024" },
        };
        prompt.StyleReference.Should().Be("anime");

        var validation = new AssetValidation
        {
            IsValid = false,
            PassedChecks = new[] { "size" },
            FailedChecks = new[] { "style" },
            Warnings = new[] { "color" },
        };
        validation.FailedChecks.Should().ContainSingle();
    }

    [Fact]
    public void ConfigurationValidator_reports_errors_and_warnings()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ashlar:Orchestration:MaxRetries"] = "11",
                ["Ashlar:Orchestration:DefaultTimeout"] = "30",
                ["Ashlar:Orchestration:UseLLM"] = "true",
            })
            .Build();

        var result = new ConfigurationValidator(config, NullLogger<ConfigurationValidator>.Instance).Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Warnings.Should().NotBeEmpty();

        var good = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ashlar:Orchestration:MaxRetries"] = "3",
                ["Ashlar:Orchestration:DefaultTimeout"] = "60",
            })
            .Build();
        new ConfigurationValidator(good).Validate().IsValid.Should().BeTrue();
    }
}
