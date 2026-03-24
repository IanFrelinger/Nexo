using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for pipeline template validation and execution.
/// </summary>
public sealed class PipelineCommand
{
    private readonly IPipelineTemplateValidator _validator;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<PipelineCommand> _logger;

    public PipelineCommand(
        IPipelineTemplateValidator validator,
        IPipelineOrchestrator orchestrator,
        IConsoleRenderer renderer,
        ILogger<PipelineCommand> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Validate(string templatePath, bool json, bool verbose)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException("Template path is required.", nameof(templatePath));

        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: validating pipeline template {templatePath}");

        try
        {
            var template = LoadTemplate(templatePath);
            var result = _validator.Validate(template);
            if (json)
            {
                _renderer.RenderJson(new
                {
                    ok = result.IsValid,
                    correlationId,
                    data = new
                    {
                        templateId = template.TemplateId,
                        isValid = result.IsValid,
                        errors = result.Errors
                    }
                });
            }
            else if (result.IsValid)
            {
                _renderer.RenderSuccess($"Pipeline template '{template.TemplateId}' is valid.");
            }
            else
            {
                _renderer.RenderError("Pipeline template is invalid:");
                foreach (var error in result.Errors)
                    _renderer.RenderError($"  - {error}");
            }

            if (verbose)
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: template validation finished");

            return result.IsValid ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline validation failed");
            if (json)
            {
                _renderer.RenderJson(new { ok = false, correlationId, error = ex.Message });
            }
            else
            {
                _renderer.RenderError($"Pipeline validation failed: {ex.Message}");
            }
            return (int)ExitCode.UnexpectedError;
        }
    }

    public async Task<int> RunAsync(
        string templatePath,
        bool json,
        bool verbose,
        string? runId,
        string? resumeRunId,
        bool resumeFailedStages,
        IReadOnlyDictionary<string, string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException("Template path is required.", nameof(templatePath));

        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: running pipeline template {templatePath}");

        try
        {
            var template = LoadTemplate(templatePath);
            var run = await _orchestrator.RunAsync(new PipelineExecutionRequest
            {
                RunId = runId,
                ResumeRunId = resumeRunId,
                ResumeFailedStages = resumeFailedStages,
                Inputs = inputs,
                Template = template
            }, cancellationToken);

            if (json)
            {
                _renderer.RenderJson(new
                {
                    ok = run.State == PipelineRunState.Completed,
                    correlationId,
                    data = new
                    {
                        runId = run.RunId,
                        templateId = run.TemplateId,
                        state = run.State.ToString(),
                        startedAt = run.StartedAt,
                        completedAt = run.CompletedAt,
                        stages = run.StageRuns.Select(s => new
                        {
                            stageId = s.StageId,
                            state = s.State.ToString(),
                            attempt = s.Attempt,
                            workerType = s.WorkerType?.ToString(),
                            workerId = s.WorkerId,
                            error = s.Error,
                            output = s.Output
                        })
                    }
                });
            }
            else
            {
                var success = run.State == PipelineRunState.Completed;
                if (success)
                    _renderer.RenderSuccess($"Pipeline run completed: {run.RunId}");
                else
                    _renderer.RenderError($"Pipeline run ended with state {run.State}: {run.RunId}");

                foreach (var stage in run.StageRuns.OrderBy(s => s.StageId, StringComparer.Ordinal))
                {
                    var line = $"{stage.StageId}: {stage.State} (attempt={stage.Attempt}, worker={stage.WorkerType?.ToString() ?? "n/a"})";
                    if (stage.State == PipelineStageRunState.Failed)
                    {
                        _renderer.RenderError($"  - {line} :: {stage.Error}");
                    }
                    else
                    {
                        _renderer.RenderSuccess($"  - {line}");
                    }
                }
            }

            if (verbose)
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: pipeline run finished");

            return run.State == PipelineRunState.Completed
                ? (int)ExitCode.Ok
                : (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline run failed");
            if (json)
            {
                _renderer.RenderJson(new { ok = false, correlationId, error = ex.Message });
            }
            else
            {
                _renderer.RenderError($"Pipeline run failed: {ex.Message}");
            }
            return (int)ExitCode.UnexpectedError;
        }
    }

    private static PipelineTemplate LoadTemplate(string templatePath)
    {
        var fullPath = Path.GetFullPath(templatePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Template file not found: {fullPath}", fullPath);

        var json = File.ReadAllText(fullPath);
        var template = JsonSerializer.Deserialize<PipelineTemplate>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
        if (template == null)
            throw new InvalidOperationException("Template JSON could not be parsed.");

        return template;
    }
}
