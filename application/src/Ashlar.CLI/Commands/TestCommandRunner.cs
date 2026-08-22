using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.UseCases.RunTests;

namespace Ashlar.CLI.Commands;

/// <summary>
/// CLI command implementation for running the internal test suite.
/// </summary>
public sealed class TestCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<TestCommand> _logger;

    /// <summary>Creates a new TestCommand instance.</summary>
    public TestCommand(IMediator mediator, IConsoleRenderer renderer, ILogger<TestCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(string? filter, bool json, bool verbose)
        => ExecuteAsync(filter, json, verbose, CancellationToken.None);

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(string? filter, bool json, bool verbose, CancellationToken ct)
    {
        try
        {
            Progress<ProgressReport>? progress = null;
            if (verbose)
            {
                _renderer.RenderProgressStart("Running tests...");
                progress = new Progress<ProgressReport>(report =>
                {
                    // In JSON mode we keep stdout clean (tests may parse it),
                    // but still allow progress to be logged if needed.
                    if (json)
                    {
                        _logger.LogInformation("Progress: {Percentage}% - {Message}", report.Percentage, report.Message);
                    }
                    else
                    {
                        _renderer.RenderProgress(report);
                    }
                });
            }

            using var globalTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            globalTimeoutCts.CancelAfter(TimeSpan.FromMinutes(10));
            var result = await _mediator.Send(new RunTestsCommand(filter, progress), globalTimeoutCts.Token);

            if (json)
            {
                // Keep behavior compatible with existing CLI tests (writes JSON to stdout).
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                if (result.FailedTests > 0)
                {
                    _renderer.RenderError($"Tests failed ({result.FailedTests}/{result.TotalTests})");
                }
                else
                {
                    _renderer.RenderSuccess($"Tests passed ({result.PassedTests}/{result.TotalTests})");
                }
            }

            if (verbose)
            {
                _renderer.RenderProgressComplete("Test run complete");
            }

            return result.FailedTests > 0 ? (int)ExitCode.ValidationFailed : (int)ExitCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test run failed");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

