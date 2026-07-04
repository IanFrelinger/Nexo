using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for analyzing code and assemblies.
/// 
/// Provides the `nexo analyze` command that:
/// - Analyzes code and assemblies in the specified directory
/// - Detects violations using analysis rules
/// - Displays results in human-readable or JSON format
/// - Shows progress updates (if verbose)
/// - Handles errors and provides appropriate exit codes
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class AnalyzeCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<AnalyzeCommand> _logger;

    /// <summary>Creates a new AnalyzeCommand instance.</summary>
    public AnalyzeCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<AnalyzeCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the analyze command.
    /// </summary>
    /// <param name="path">Directory path to analyze</param>
    /// <param name="json">Whether to output JSON format</param>
    /// <param name="verbose">Whether to show verbose progress output</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
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
            var progress = CommandExecutionSupport.CreateProgressReporter(
                verbose,
                json,
                _logger,
                _renderer);

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
            return CommandExecutionSupport.RenderDomainFailure(
                _logger,
                _renderer,
                ex,
                "Analysis failed",
                (int)ExitCode.ValidationFailed);
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
}

