using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Common.Models;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for running tests.
/// </summary>
public class TestCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<TestCommand> _logger;

    public TestCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<TestCommand> logger)
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
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: running tests (filter={filter ?? "none"})");
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

            var command = new RunTestsCommand(filter, progress);
            var result = await _mediator.Send(command);

            if (json)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                Console.Out.WriteLine(JsonSerializer.Serialize(result, options));
            }
            else
            {
                Console.Out.WriteLine($"Tests: {result.PassedTests}/{result.TotalTests} passed");
                Console.Out.WriteLine($"Duration: {result.TotalDuration.TotalMilliseconds}ms");
                
                if (result.FailedTests > 0)
                {
                    Console.Error.WriteLine("\nFailed tests:");
                    foreach (var test in result.Results.Where(r => !r.Passed))
                    {
                        Console.Error.WriteLine($"  - {test.TestName} ({test.Category})");
                        if (!string.IsNullOrEmpty(test.ErrorMessage))
                        {
                            Console.Error.WriteLine($"    Error: {test.ErrorMessage}");
                        }
                    }
                }
            }

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: tests completed");
            }

            return result.FailedTests == 0 ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during test execution");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

