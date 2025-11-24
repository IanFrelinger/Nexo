using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
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

    public async Task<int> ExecuteAsync(string? filter, bool json)
    {
        try
        {
            var command = new RunValidationCommand(filter);
            var result = await _mediator.Send(command);

            _renderer.RenderValidationResult(result, json);

            return result.Passed ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation failed");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

