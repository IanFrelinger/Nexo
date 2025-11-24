using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for analyzing code and assemblies.
/// </summary>
public class AnalyzeCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<AnalyzeCommand> _logger;

    public AnalyzeCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<AnalyzeCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(DirectoryInfo path, bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: analyzing {path.FullName}");
        }

        try
        {
            Progress<ProgressReport>? progress = null;
            if (verbose || !json)
            {
                progress = new Progress<ProgressReport>(report =>
                {
                    if (json)
                    {
                        // In JSON mode, only log to stderr
                        _logger.LogInformation(
                            "Progress: {Percentage}% - {Message}",
                            report.Percentage,
                            report.Message);
                    }
                    else
                    {
                        // In normal mode, show progress on stdout
                        _renderer.RenderProgress(report);
                    }
                });
            }

            var command = new AnalyzeCodeCommand(path, progress);
            var result = await _mediator.Send(command);

            _renderer.RenderAnalysisResult(result, json);

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: analysis completed");
            }

            return result.HasViolations ? (int)ExitCode.ValidationFailed : (int)ExitCode.Ok;
        }
        catch (AnalysisException ex)
        {
            return HandleAnalysisException(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Policy violation during analysis");
            _renderer.RenderError($"Policy violation: {ex.Message}");
            return (int)ExitCode.PolicyViolation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during analysis");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }

    private int HandleAnalysisException(AnalysisException ex)
    {
        _logger.LogError(ex, "Analysis failed");
        if (!string.IsNullOrEmpty(ex.ErrorCode))
        {
            _renderer.RenderErrorWithCode(ex.Message, ex.ErrorCode, ex.Suggestion);
        }
        else
        {
            _renderer.RenderError(ex.Message);
        }
        return (int)ExitCode.ValidationFailed;
    }
}

