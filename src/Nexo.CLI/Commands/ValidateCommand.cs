using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for running validation tests.
/// </summary>
public class ValidateCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ValidateCommand> _logger;

    public ValidateCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ValidateCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string? filter, bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: validating (filter={filter ?? "none"})");
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
                        _logger.LogInformation(
                            "Progress: {Percentage}% - {Message}",
                            report.Percentage,
                            report.Message);
                    }
                    else
                    {
                        _renderer.RenderProgress(report);
                    }
                });
            }

            var command = new RunValidationCommand(filter, progress);
            var result = await _mediator.Send(command);

            _renderer.RenderValidationResult(result, json);

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: validation finished");
            }

            return result.Passed ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (ValidationException ex)
        {
            return HandleValidationException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }

    private int HandleValidationException(ValidationException ex)
    {
        _logger.LogError(ex, "Validation failed");
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

