using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Configuration.UseCases.GetConfiguration;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for managing configuration.
/// </summary>
public class ConfigCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ConfigCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: loading configuration");
        }

        try
        {
            var query = new GetConfigurationQuery();
            var config = await _mediator.Send(query);

            if (json)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                Console.Out.WriteLine(JsonSerializer.Serialize(config, options));
            }
            else
            {
                Console.Out.WriteLine("Current Configuration:");
                Console.Out.WriteLine($"  Analysis:");
                Console.Out.WriteLine($"    Enabled Rules: {string.Join(", ", config.Analysis.EnabledRules)}");
                Console.Out.WriteLine($"    Max Complexity: {config.Analysis.MaxComplexityThreshold}");
                Console.Out.WriteLine($"    Security Scan: {config.Analysis.EnableSecurityScan}");
                Console.Out.WriteLine($"    Code Quality: {config.Analysis.EnableCodeQuality}");
                Console.Out.WriteLine($"  Validation:");
                Console.Out.WriteLine($"    Timeout: {config.Validation.TimeoutSeconds}s");
                Console.Out.WriteLine($"    Fail on No Tests: {config.Validation.FailOnNoTests}");
                Console.Out.WriteLine($"  Logging:");
                Console.Out.WriteLine($"    Level: {config.Logging.Level}");
                Console.Out.WriteLine($"    Structured Logging: {config.Logging.EnableStructuredLogging}");
            }

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: configuration loaded");
            }

            return (int)ExitCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting configuration");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

