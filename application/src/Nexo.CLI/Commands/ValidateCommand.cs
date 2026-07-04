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
/// 
/// Provides the `nexo validate` command that:
/// - Runs architecture and contract validation tests
/// - Supports optional test filtering
/// - Displays results in human-readable or JSON format
/// - Shows progress updates (if verbose)
/// - Handles errors and provides appropriate exit codes
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class ValidateCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ValidateCommand> _logger;

    /// <summary>Creates a new ValidateCommand instance.</summary>
    public ValidateCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ValidateCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
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
            var progress = CommandExecutionSupport.CreateProgressReporter(
                verbose,
                json,
                _logger,
                _renderer);

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
            return CommandExecutionSupport.RenderDomainFailure(
                _logger,
                _renderer,
                ex,
                "Validation failed",
                (int)ExitCode.ValidationFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

