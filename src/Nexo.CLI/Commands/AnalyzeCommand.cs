using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

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

    public async Task<int> ExecuteAsync(DirectoryInfo path, bool json)
    {
        try
        {
            var command = new AnalyzeCodeCommand(path);
            var result = await _mediator.Send(command);

            _renderer.RenderAnalysisResult(result, json);

            return result.HasViolations ? (int)ExitCode.ValidationFailed : (int)ExitCode.Ok;
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

