using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Configuration.UseCases.GetConfiguration;
using Ashlar.Core.Application.Execution.Models;
using Ashlar.Core.Application.Execution.Ports;
using System.Text.Json;

namespace Ashlar.CLI.Commands;

/// <summary>
/// CLI command for managing configuration.
/// 
/// Provides the `ashlar config` command that:
/// - Displays current configuration settings (show)
/// - Validates configuration (validate)
/// - Exports/imports configuration (export, import)
/// - Sets step execution mode (set-mode, hot-swap)
/// - Outputs in human-readable or JSON format
/// </summary>
public class ConfigCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ConfigCommand> _logger;
    private readonly BackgroundAgentConfigLoader? _backgroundAgentConfigLoader;
    private readonly IStepExecutionMode? _stepExecutionMode;
    private readonly IAdaptationAuditLog? _auditLog;

    /// <summary>Creates a new ConfigCommand instance.</summary>
    public ConfigCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ConfigCommand> logger,
        BackgroundAgentConfigLoader? backgroundAgentConfigLoader = null,
        IStepExecutionMode? stepExecutionMode = null,
        IAdaptationAuditLog? auditLog = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backgroundAgentConfigLoader = backgroundAgentConfigLoader;
        _stepExecutionMode = stepExecutionMode;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Sets execution mode for a step (hot-swap). Takes effect on next execution.
    /// Logs the mode change to the adaptation audit log when available.
    /// </summary>
    public async Task<int> SetModeAsync(string stepId, ExecutionMode mode)
    {
        if (_stepExecutionMode == null)
        {
            _logger.LogWarning("StepExecutionMode not registered; using local store");
            var store = new Ashlar.Infrastructure.Execution.StepExecutionModeStore();
            var result = await store.SwapAsync(stepId, mode);
            Console.WriteLine($"Swapped {stepId}: {result.PreviousMode} -> {result.NewMode}");
            return 0;
        }

        var result2 = await _stepExecutionMode.SwapAsync(stepId, mode);
        Console.WriteLine($"Swapped {stepId}: {result2.PreviousMode} -> {result2.NewMode}");

        if (_auditLog != null)
        {
            await _auditLog.LogAsync(new AdaptationAuditEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                AutonomyLevel = "config",
                Outcome = "HotSwap",
                BrickId = stepId,
                FailureType = "ModeChange",
                FilePath = null,
                RegressionPassed = false,
                Promoted = false,
                Message = $"Hot-swap: {stepId} {result2.PreviousMode} -> {result2.NewMode}",
            });
        }

        return 0;
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
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

    /// <summary>
    /// Validate configuration (e.g. background agent config).
    /// </summary>
    public async Task<int> ValidateAsync(bool json, bool verbose)
    {
        try
        {
            var errors = new List<string>();
            if (_backgroundAgentConfigLoader != null)
            {
                try
                {
                    var configs = await _backgroundAgentConfigLoader.LoadAsync(default);
                    if (verbose && !json)
                        _renderer.RenderProgressComplete($"Validated {configs.Count} background agent(s)");
                }
                catch (Exception ex)
                {
                    errors.Add($"BackgroundAgents: {ex.Message}");
                }
            }
            if (errors.Count > 0)
            {
                if (json)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, errors }));
                else
                    foreach (var e in errors) Console.Error.WriteLine(e);
                return (int)ExitCode.UnexpectedError;
            }
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true }));
            else
                Console.Out.WriteLine("Configuration valid.");
            return (int)ExitCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate configuration failed");
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }

    /// <summary>
    /// Export current configuration to a file.
    /// </summary>
    public async Task<int> ExportAsync(string path, bool json, bool verbose)
    {
        try
        {
            var query = new GetConfigurationQuery();
            var config = await _mediator.Send(query);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var text = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(path, text).ConfigureAwait(false);
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, path }));
            else
                Console.Out.WriteLine($"Configuration exported to {path}");
            return (int)ExitCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export configuration failed");
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }

    /// <summary>
    /// Import configuration from a file (validates format only; does not apply).
    /// </summary>
    public async Task<int> ImportAsync(string path, bool json, bool verbose)
    {
        try
        {
            if (!File.Exists(path))
            {
                if (json)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"File not found: {path}" }));
                else
                    _renderer.RenderError($"File not found: {path}");
                return (int)ExitCode.UnexpectedError;
            }
            var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            using var _ = JsonDocument.Parse(text);
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, path, message = "Format valid" }));
            else
                Console.Out.WriteLine($"Configuration file valid: {path}");
            return (int)ExitCode.Ok;
        }
        catch (JsonException ex)
        {
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Invalid JSON: {ex.Message}" }));
            else
                _renderer.RenderError($"Invalid JSON: {ex.Message}");
            return (int)ExitCode.UnexpectedError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import configuration failed");
            if (json)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

