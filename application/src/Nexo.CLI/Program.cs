using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using FluentValidation;
using Nexo.CLI.Commands;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Hosting;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Persistence;
using Nexo.Runtime;
using Nexo.Transport.Grpc;

namespace Nexo.CLI;

/// <summary>
/// Main entry point for the Nexo CLI application.
///
/// Provides command-line interface for:
/// - Code analysis and validation
/// - Agent execution and orchestration
/// - Test execution
/// - Configuration management
/// - Escalation and conflict resolution
/// - Metrics and performance monitoring
///
/// Uses System.CommandLine for command parsing and Microsoft.Extensions.Hosting
/// for dependency injection and service configuration.
///
/// Light commands (improve, adapt, dogfood, observe, compose, mesh, self-context, docker, test portable/parallel/multi-env)
/// build their own ServiceProvider and do not load Nexo.Orchestration. The host is built lazily only when
/// a heavy command (analyze, validate, agent, etc.) is invoked, avoiding FileNotFoundException for orchestration.
/// </summary>
static partial class Program
{
    private static readonly Lazy<IHost> Host = new(() =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build());

    private static IServiceProvider ServiceProvider => Host.Value.Services;

    /// <summary>
    /// Main entry point for the CLI application.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    static async Task<int> Main(string[] args)
    {
        var root = BuildRootCommand();
        return await root.InvokeAsync(args);
    }

    /// <summary>
    /// Configures dependency injection services for the application.
    /// Registers the Nexo kernel via AddNexo(), then CLI-specific commands and adapters.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<GrpcTransportOptions>(
            context.Configuration.GetSection("Nexo:GrpcTransport"));
        services.AddNexoRuntimeRouting(context.Configuration);
        services.AddNexo();
        services.TryAddSingleton<Nexo.Core.Application.SelfImprovement.Ports.ISelfImprovementMetricsStore>(
            _ => new Nexo.Infrastructure.SelfImprovement.FileBasedSelfImprovementMetricsStore());

        // Dog-food: optimizer agents run the app's own analysis pipeline
        services.TryAddSingleton<Nexo.BackgroundAgents.Optimization.ICodeAnalysisRunner, Nexo.BackgroundAgents.HostRunners.CodeAnalysisRunnerAdapter>();
        // Dog-food: tester agents run the app's own test pipeline
        services.TryAddSingleton<Nexo.BackgroundAgents.Testing.ITestRunRunner, Nexo.BackgroundAgents.HostRunners.TestRunRunnerAdapter>();
        // Dog-food: extender agents run self-extend cycle (LLM + tools with path policy)
        services.TryAddSingleton<Nexo.BackgroundAgents.HostRunners.SelfExtendRunnerAdapter>();
        services.TryAddSingleton<Nexo.BackgroundAgents.Extending.ISelfExtendRunner>(
            sp => sp.GetRequiredService<Nexo.BackgroundAgents.HostRunners.SelfExtendRunnerAdapter>());

        // Register CLI commands
        services.AddScoped<AnalyzeCommand>();
        services.AddScoped<ValidateCommand>();
        services.AddScoped<AgentCommand>();
        services.AddScoped<ListAgentsCommand>();
        services.AddScoped<ConfigCommand>();
        services.AddScoped<TestCommand>();
        services.AddScoped<OrchestrateCommand>();
        services.AddScoped<EscalateCommand>();
        services.AddScoped<MetricsCommand>();
        services.AddScoped<PipelineCommand>();
        services.AddScoped<MaintenanceCommand>();
        services.AddScoped<BackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.BackgroundAgentDaemonCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ExecuteBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.LogsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.MetricsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.StatsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ObservationsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ModeBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.OperatorDashboardBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
        services.AddScoped<TrustCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.WebSearchCommand>();

        // CLI-specific: console renderer for output formatting
        services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();
    }

    private static TimeSpan? ParseSince(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return null;
        since = since.Trim();
        if (since.Length < 2)
            return null;
        var unit = since[^1];
        if (!int.TryParse(since[..^1], out var value) || value <= 0)
            return null;
        return unit switch
        {
            'h' or 'H' => TimeSpan.FromHours(value),
            'm' or 'M' => TimeSpan.FromMinutes(value),
            's' or 'S' => TimeSpan.FromSeconds(value),
            'd' or 'D' => TimeSpan.FromDays(value),
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string> ParseHeaders(IReadOnlyList<string> rawHeaders)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in rawHeaders)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var separator = raw.IndexOf(':');
            if (separator <= 0 || separator == raw.Length - 1)
                continue;

            var key = raw[..separator].Trim();
            var value = raw[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            headers[key] = value;
        }

        return headers;
    }

    private static IReadOnlyDictionary<string, string> ParsePipelineInputs(string[]? rawInputs)
    {
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (rawInputs == null || rawInputs.Length == 0)
            return inputs;

        foreach (var raw in rawInputs)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var index = raw.IndexOf('=');
            if (index <= 0 || index == raw.Length - 1)
                throw new ArgumentException($"Invalid --input '{raw}'. Expected key=value format.");

            var key = raw[..index].Trim();
            var value = raw[(index + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException($"Invalid --input '{raw}'. Key cannot be empty.");

            inputs[key] = value;
        }

        return inputs;
    }
}
