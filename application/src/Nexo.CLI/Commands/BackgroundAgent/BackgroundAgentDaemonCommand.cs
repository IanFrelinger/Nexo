using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Testing;
using Nexo.Hosting;
using Nexo.Hosting.Sdk.Extensions;
using Nexo.Runtime;
using Nexo.Transport.Grpc;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Runs a long-lived host process for background agents.
/// This enables daemon-like execution from the CLI entrypoint.
/// </summary>
public sealed class BackgroundAgentDaemonCommand
{
    /// <summary>Creates a new RunAsync instance.</summary>
    public async Task<int> RunAsync(
        string? configPath,
        string? duration,
        string? patternStorePath,
        bool disableObservation,
        bool formatJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedConfigPath = string.IsNullOrWhiteSpace(configPath) ? null : configPath.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedConfigPath) && !File.Exists(trimmedConfigPath))
            {
                return WriteError(formatJson, $"Config file not found: {trimmedConfigPath}");
            }

            if (!TryParseDuration(duration, out var runDuration))
            {
                return WriteError(formatJson, $"Invalid --duration value '{duration}'. Use formats like 30s, 5m, or 1h.");
            }

            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Insert(0, new MemoryConfigurationSource
                    {
                        InitialData = new Dictionary<string, string?>
                        {
                            // Provide safe defaults so daemon mode can run even when host-level
                            // barrier configuration is not explicitly supplied.
                            ["Nexo:Barriers:Levels:0"] = "public",
                            ["Nexo:Barriers:Levels:1"] = "internal",
                            ["Nexo:Barriers:RequireExplicitBarrier"] = "false"
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(trimmedConfigPath))
                    {
                        config.AddJsonFile(trimmedConfigPath!, optional: false, reloadOnChange: true);
                    }
                })
                // Host.CreateDefaultBuilder already wires the console provider; this only switches it
                // to JSON lines when Nexo:Logging:Json=true or NEXO_LOG_JSON=1 (same flag as Nexo.API).
                .ConfigureLogging((context, logging) =>
                    logging.AddNexoJsonConsoleIfRequested(context.Configuration))
                .ConfigureServices((context, services) =>
                {
                    services.Configure<GrpcTransportOptions>(
                        context.Configuration.GetSection("Nexo:GrpcTransport"));
                    services.AddNexoRuntimeRouting(context.Configuration);
                    services.AddNexo(options =>
                    {
                        options.PatternStorePath = string.IsNullOrWhiteSpace(patternStorePath)
                            ? context.Configuration["Nexo:PatternStorePath"]
                            : patternStorePath;
                        options.RegisterBackgroundAgentHostedService = true;
                        options.DisableObservationPipeline = disableObservation;
                    });

                    // Keep daemon host behavior aligned with the CLI's adapter stack.
                    services.TryAddSingleton<ICodeAnalysisRunner, CodeAnalysisRunnerAdapter>();
                    services.TryAddSingleton<ITestRunRunner, TestRunRunnerAdapter>();
                    services.TryAddSingleton<SelfExtendRunnerAdapter>();
                    services.TryAddSingleton<ISelfExtendRunner>(sp =>
                        sp.GetRequiredService<SelfExtendRunnerAdapter>());
                });

            using var host = builder.Build();
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            WriteStarted(formatJson, runDuration, disableObservation);

            if (runDuration.HasValue)
            {
                await Task.Delay(runDuration.Value, cancellationToken).ConfigureAwait(false);
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
                WriteStopped(formatJson, timedOut: true);
                return 0;
            }

            await host.WaitForShutdownAsync(cancellationToken).ConfigureAwait(false);
            WriteStopped(formatJson, timedOut: false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            WriteStopped(formatJson, timedOut: false);
            return 0;
        }
        catch (Exception ex)
        {
            return WriteError(formatJson, $"Background agent daemon failed: {ex.Message}");
        }
    }

    private static bool TryParseDuration(string? value, out TimeSpan? duration)
    {
        duration = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var input = value.Trim();
        if (input.Length < 2)
            return false;

        var unit = input[^1];
        if (!double.TryParse(input[..^1], out var magnitude) || magnitude <= 0)
            return false;

        duration = unit switch
        {
            's' or 'S' => TimeSpan.FromSeconds(magnitude),
            'm' or 'M' => TimeSpan.FromMinutes(magnitude),
            'h' or 'H' => TimeSpan.FromHours(magnitude),
            'd' or 'D' => TimeSpan.FromDays(magnitude),
            _ => (TimeSpan?)null
        };

        return duration.HasValue;
    }

    private static void WriteStarted(bool formatJson, TimeSpan? runDuration, bool disableObservation)
    {
        if (formatJson)
        {
            Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = true,
                status = "running",
                observation = disableObservation ? "disabled" : "enabled",
                duration = runDuration?.ToString()
            }));
            return;
        }

        var durationText = runDuration.HasValue
            ? $" for {runDuration.Value}"
            : " until Ctrl+C";
        Console.Out.WriteLine($"Background-agent daemon started{durationText}.");
    }

    private static void WriteStopped(bool formatJson, bool timedOut)
    {
        if (formatJson)
        {
            Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = true,
                status = "stopped",
                reason = timedOut ? "duration_elapsed" : "shutdown"
            }));
            return;
        }

        Console.Out.WriteLine("Background-agent daemon stopped.");
    }

    private static int WriteError(bool formatJson, string error)
    {
        if (formatJson)
        {
            Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                ok = false,
                error
            }));
        }
        else
        {
            Console.Error.WriteLine(error);
        }

        return 1;
    }
}
