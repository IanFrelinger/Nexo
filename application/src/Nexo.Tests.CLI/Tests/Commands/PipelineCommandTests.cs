using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Infrastructure.Pipelines;
using Microsoft.Extensions.Options;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class PipelineCommandTests : UnitTestBase
{
    private string? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nexo-pipeline-cmd-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_tempDir) && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestValidateSuccess();
            await TestValidateFailure();
            await TestRunSuccess();
            await TestRunFailure();
            await TestRunWithResumeAndInputs();
            await TestDiagnostics();

            return new TestResult
            {
                Name = nameof(PipelineCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All PipelineCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(PipelineCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(PipelineCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestValidateSuccess()
    {
        var templatePath = WriteTemplate(new PipelineTemplate
        {
            TemplateId = "validate-ok",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "stage-a", Name = "Stage A", Mode = PipelineExecutionMode.Deterministic }
            }
        }, "validate-ok.json");

        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>(MockBehavior.Strict);
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions());
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions());
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions());
        var logger = new Mock<ILogger<PipelineCommand>>();

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);
        var exitCode = command.Validate(templatePath, json: false, verbose: false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        renderer.Verify(x => x.RenderSuccess(It.Is<string>(msg => msg.Contains("is valid"))), Times.Once);
        return Task.CompletedTask;
    }

    private Task TestValidateFailure()
    {
        var templatePath = WriteTemplate(new PipelineTemplate
        {
            TemplateId = "validate-fail",
            Stages = Array.Empty<PipelineStageDefinition>()
        }, "validate-fail.json");

        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>(MockBehavior.Strict);
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions());
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions());
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions());
        var logger = new Mock<ILogger<PipelineCommand>>();

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);
        var exitCode = command.Validate(templatePath, json: false, verbose: false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        renderer.Verify(x => x.RenderError(It.Is<string>(msg => msg.Contains("invalid", StringComparison.OrdinalIgnoreCase))), Times.AtLeastOnce);
        return Task.CompletedTask;
    }

    private async Task TestRunSuccess()
    {
        var templatePath = WriteTemplate(new PipelineTemplate
        {
            TemplateId = "run-ok",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "stage-a", Name = "Stage A", Mode = PipelineExecutionMode.Deterministic }
            }
        }, "run-ok.json");

        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>();
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions());
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions());
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions());
        var logger = new Mock<ILogger<PipelineCommand>>();

        orchestrator
            .Setup(x => x.RunAsync(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRun
            {
                RunId = "run-1",
                TemplateId = "run-ok",
                State = PipelineRunState.Completed,
                StageRuns = new[]
                {
                    new PipelineStageRun
                    {
                        StageId = "stage-a",
                        State = PipelineStageRunState.Completed,
                        Attempt = 1,
                        WorkerType = PipelineWorkerType.Deterministic
                    }
                }
            });

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);
        var exitCode = await command.RunAsync(
            templatePath,
            json: false,
            verbose: false,
            runId: "run-1",
            resumeRunId: null,
            resumeFailedStages: false,
            inputs: new Dictionary<string, string>());

        AssertEqual((int)ExitCode.Ok, exitCode);
        renderer.Verify(x => x.RenderSuccess(It.Is<string>(msg => msg.Contains("Pipeline run completed", StringComparison.OrdinalIgnoreCase))), Times.AtLeastOnce);
    }

    private async Task TestRunFailure()
    {
        var templatePath = WriteTemplate(new PipelineTemplate
        {
            TemplateId = "run-failed",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "stage-a", Name = "Stage A", Mode = PipelineExecutionMode.Deterministic }
            }
        }, "run-failed.json");

        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>();
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions());
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions());
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions());
        var logger = new Mock<ILogger<PipelineCommand>>();

        orchestrator
            .Setup(x => x.RunAsync(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRun
            {
                RunId = "run-2",
                TemplateId = "run-failed",
                State = PipelineRunState.Failed,
                StageRuns = new[]
                {
                    new PipelineStageRun
                    {
                        StageId = "stage-a",
                        State = PipelineStageRunState.Failed,
                        Attempt = 1,
                        WorkerType = PipelineWorkerType.Deterministic,
                        Error = "boom"
                    }
                }
            });

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);
        var exitCode = await command.RunAsync(
            templatePath,
            json: false,
            verbose: false,
            runId: "run-2",
            resumeRunId: null,
            resumeFailedStages: false,
            inputs: new Dictionary<string, string>());

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        renderer.Verify(x => x.RenderError(It.Is<string>(msg => msg.Contains("ended with state", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    private async Task TestRunWithResumeAndInputs()
    {
        var templatePath = WriteTemplate(new PipelineTemplate
        {
            TemplateId = "run-resume",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "stage-a", Name = "Stage A", Mode = PipelineExecutionMode.Deterministic }
            }
        }, "run-resume.json");

        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>();
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions());
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions());
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions());
        var logger = new Mock<ILogger<PipelineCommand>>();

        PipelineExecutionRequest? captured = null;
        orchestrator
            .Setup(x => x.RunAsync(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineExecutionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PipelineRun
            {
                RunId = "run-3",
                TemplateId = "run-resume",
                State = PipelineRunState.Completed,
                StageRuns = Array.Empty<PipelineStageRun>()
            });

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);
        var exitCode = await command.RunAsync(
            templatePath,
            json: true,
            verbose: false,
            runId: "run-3",
            resumeRunId: "prior-1",
            resumeFailedStages: true,
            inputs: new Dictionary<string, string> { ["k"] = "v" });

        AssertEqual((int)ExitCode.Ok, exitCode);
        AssertNotNull(captured);
        AssertEqual("run-3", captured!.RunId);
        AssertEqual("prior-1", captured.ResumeRunId);
        AssertTrue(captured.ResumeFailedStages);
        AssertEqual("v", captured.Inputs["k"]);
        renderer.Verify(x => x.RenderJson(It.IsAny<object>()), Times.Once);
    }

    private Task TestDiagnostics()
    {
        var validator = new PipelineTemplateValidator();
        var orchestrator = new Mock<IPipelineOrchestrator>(MockBehavior.Strict);
        var renderer = new Mock<IConsoleRenderer>();
        var executionOptions = Options.Create(new PipelineExecutionOptions
        {
            MaxRetryAttempts = 3,
            RetryDelayMs = 25,
            CompletionPolicy = PipelineCompletionPolicy.FailOnAnyStageFailure
        });
        var persistenceOptions = Options.Create(new PipelinePersistenceOptions
        {
            Provider = "InMemory",
            DatabasePath = "/tmp/test.db"
        });
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions
        {
            DeterministicAdapter = "default",
            AgenticAdapter = "default"
        });
        var logger = new Mock<ILogger<PipelineCommand>>();

        var command = new PipelineCommand(
            validator,
            orchestrator.Object,
            renderer.Object,
            executionOptions,
            persistenceOptions,
            adapterOptions,
            logger.Object);

        var exitCode = command.Diagnostics(json: true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        renderer.Verify(x => x.RenderJson(It.IsAny<object>()), Times.Once);
        return Task.CompletedTask;
    }

    private string WriteTemplate(PipelineTemplate template, string filename)
    {
        var path = Path.Combine(_tempDir!, filename);
        var json = System.Text.Json.JsonSerializer.Serialize(template, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
        return path;
    }
}
