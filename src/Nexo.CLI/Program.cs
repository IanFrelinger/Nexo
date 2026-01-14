using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using FluentValidation;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Abstractions;
using Nexo.Orchestration;

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
/// - Unity integration
/// 
/// Uses System.CommandLine for command parsing and Microsoft.Extensions.Hosting
/// for dependency injection and service configuration.
/// </summary>
static class Program
{
    /// <summary>
    /// Main entry point for the CLI application.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    static async Task<int> Main(string[] args)
    {
        // Build host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

        var root = new RootCommand("Nexo command-line interface")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        // Global options
        var jsonOpt = new Option<bool>(
            name: "--format-json",
            description: "Emit machine-readable JSON (stderr still carries logs)."
        );

        root.AddGlobalOption(jsonOpt);

        var verboseOpt = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose logging and progress output."
        );
        root.AddGlobalOption(verboseOpt);

        // Get services from DI container
        var serviceProvider = host.Services;
        var analyzeCommand = serviceProvider.GetRequiredService<AnalyzeCommand>();
        var validateCommand = serviceProvider.GetRequiredService<ValidateCommand>();
        var agentCommand = serviceProvider.GetRequiredService<AgentCommand>();

        // nexo analyze
        var analyzeCmd = new Command("analyze", "Run code/assembly analyzers and policies")
        {
            new Option<DirectoryInfo>(
                name: "--path",
                description: "Root path to analyze (defaults to current)",
                getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory)
            )
        };
        analyzeCmd.SetHandler(
            async (DirectoryInfo path, bool json, bool verbose) =>
            {
                var exitCode = await analyzeCommand.ExecuteAsync(path, json, verbose);
                Environment.Exit(exitCode);
            },
            analyzeCmd.Options[0] as Option<DirectoryInfo> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);

        // nexo validate
        var validateCmd = new Command("validate", "Run architecture tests/contract checks quickly")
        {
            new Option<string?>("--filter", "Optional test filter (Category/Trait)")
        };
        validateCmd.SetHandler(
            async (string? filter, bool json, bool verbose) =>
            {
                var exitCode = await validateCommand.ExecuteAsync(filter, json, verbose);
                Environment.Exit(exitCode);
            },
            validateCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);

        // nexo agent
        var agentCmd = new Command("agent", "Run an agent action")
        {
            new Option<string>("--name", "Agent name") { IsRequired = true },
            new Option<FileInfo?>("--input", "Optional input file")
        };
        agentCmd.SetHandler(
            async (string name, FileInfo? input, bool json, bool verbose) =>
            {
                var exitCode = await agentCommand.ExecuteAsync(name, input, json, verbose);
                Environment.Exit(exitCode);
            },
            agentCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            agentCmd.Options[1] as Option<FileInfo?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);

        // nexo agent list
        var listAgentsCommand = serviceProvider.GetRequiredService<ListAgentsCommand>();
        var listAgentsCmd = new Command("list", "List available agents");
        listAgentsCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await listAgentsCommand.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        agentCmd.AddCommand(listAgentsCmd);

        // nexo config
        var configCommand = serviceProvider.GetRequiredService<ConfigCommand>();
        var configCmd = new Command("config", "View or manage configuration");
        configCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);

        // nexo test - TODO: Re-implement test runner command
        // This was removed when we renamed TestCommand to UniversalTestCommand
        // The old test command was for running unit tests, not the Universal Testing Agent
        // For now, use "nexo demo test" for Universal Testing Agent
        // var testCommand = serviceProvider.GetRequiredService<TestCommand>();
        // var testCmd = new Command("test", "Run tests");
        // testCmd.AddOption(new Option<string?>("--filter", "Filter tests by name or category"));
        // testCmd.SetHandler(...);

        // nexo orchestrate
        var orchestrateCommand = serviceProvider.GetService<OrchestrateCommand>();
        if (orchestrateCommand != null)
        {
            var orchestrateCmd = new Command("orchestrate", "Orchestrate agent execution for a request");
            orchestrateCmd.AddArgument(new Argument<string>("request", "The request to orchestrate"));
            orchestrateCmd.SetHandler(
                async (string request, bool json, bool verbose) =>
                {
                    var exitCode = await orchestrateCommand.ExecuteAsync(request, json, verbose);
                    Environment.Exit(exitCode);
                },
                orchestrateCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
                jsonOpt,
                verboseOpt);
            root.AddCommand(orchestrateCmd);
        }

        // nexo escalate
        var escalateCommand = serviceProvider.GetRequiredService<EscalateCommand>();
        var escalateCmd = new Command("escalate", "Manage escalations and conflicts");
        
        // nexo escalate list
        var escalateListCmd = new Command("list", "List all pending escalations");
        escalateListCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await escalateCommand.ListAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateListCmd);

        // nexo escalate show
        var escalateShowCmd = new Command("show", "Show details for a specific escalation");
        escalateShowCmd.AddArgument(new Argument<string>("id", "Escalation ID"));
        escalateShowCmd.SetHandler(
            async (string id, bool json, bool verbose) =>
            {
                var exitCode = await escalateCommand.ShowAsync(id, json, verbose);
                Environment.Exit(exitCode);
            },
            escalateShowCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateShowCmd);

        // nexo escalate resolve
        var escalateResolveCmd = new Command("resolve", "Resolve an escalation");
        escalateResolveCmd.AddArgument(new Argument<string>("id", "Escalation ID"));
        escalateResolveCmd.AddOption(new Option<string?>("--resolution", "Resolution description"));
        escalateResolveCmd.SetHandler(
            async (string id, string? resolution, bool json, bool verbose) =>
            {
                var exitCode = await escalateCommand.ResolveAsync(id, resolution, json, verbose);
                Environment.Exit(exitCode);
            },
            escalateResolveCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            escalateResolveCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateResolveCmd);

        // nexo escalate dismiss
        var escalateDismissCmd = new Command("dismiss", "Dismiss an escalation");
        escalateDismissCmd.AddArgument(new Argument<string>("id", "Escalation ID"));
        escalateDismissCmd.AddOption(new Option<string?>("--reason", "Dismissal reason"));
        escalateDismissCmd.SetHandler(
            async (string id, string? reason, bool json, bool verbose) =>
            {
                var exitCode = await escalateCommand.DismissAsync(id, reason, json, verbose);
                Environment.Exit(exitCode);
            },
            escalateDismissCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            escalateDismissCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateDismissCmd);

        // nexo escalate list-by-severity
        var escalateListBySeverityCmd = new Command("list-by-severity", "List escalations filtered by severity");
        escalateListBySeverityCmd.AddArgument(new Argument<string>("severity", "Severity level (Low, Medium, High, Critical)"));
        escalateListBySeverityCmd.SetHandler(
            async (string severity, bool json, bool verbose) =>
            {
                var exitCode = await escalateCommand.ListBySeverityAsync(severity, json, verbose);
                Environment.Exit(exitCode);
            },
            escalateListBySeverityCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateListBySeverityCmd);

        // nexo metrics
        var metricsCommand = serviceProvider.GetRequiredService<MetricsCommand>();
        var metricsCmd = new Command("metrics", "View orchestration metrics and performance data");
        
        // nexo metrics report
        var metricsReportCmd = new Command("report", "Show performance report");
        metricsReportCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await metricsCommand.ShowReportAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        metricsCmd.AddCommand(metricsReportCmd);

        // nexo metrics agent
        var metricsAgentCmd = new Command("agent", "Show metrics for a specific agent");
        metricsAgentCmd.AddArgument(new Argument<string>("id", "Agent ID"));
        metricsAgentCmd.SetHandler(
            async (string id, bool json, bool verbose) =>
            {
                var exitCode = await metricsCommand.ShowAgentAsync(id, json, verbose);
                Environment.Exit(exitCode);
            },
            metricsAgentCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        metricsCmd.AddCommand(metricsAgentCmd);

        // nexo metrics traces
        var metricsTracesCmd = new Command("traces", "Show trace spans");
        metricsTracesCmd.AddOption(new Option<string?>("--correlation-id", "Filter by correlation ID"));
        metricsTracesCmd.AddOption(new Option<string?>("--operation", "Filter by operation name"));
        metricsTracesCmd.SetHandler(
            async (string? correlationId, string? operation, bool json, bool verbose) =>
            {
                var exitCode = await metricsCommand.ShowTracesAsync(correlationId, operation, json, verbose);
                Environment.Exit(exitCode);
            },
            metricsTracesCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            metricsTracesCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        metricsCmd.AddCommand(metricsTracesCmd);

        // nexo metrics clear
        var metricsClearCmd = new Command("clear", "Clear all collected metrics");
        metricsClearCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await metricsCommand.ClearAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        metricsCmd.AddCommand(metricsClearCmd);

        // nexo unity
        var unityCmd = UnityCommand.CreateCommand(host.Services, jsonOpt, verboseOpt);
        root.AddCommand(unityCmd);

        // nexo demo
        var demoCmd = DemoCommand.CreateCommand(host.Services, jsonOpt, verboseOpt);
        root.AddCommand(demoCmd);

        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);
        root.AddCommand(configCmd);
        // root.AddCommand(testCmd); // TODO: Re-implement test runner command
        root.AddCommand(escalateCmd);
        root.AddCommand(metricsCmd);

        return await root.InvokeAsync(args);
    }

    /// <summary>
    /// Configures dependency injection services for the application.
    /// 
    /// Registers:
    /// - MediatR for command/query handling
    /// - FluentValidation for request validation
    /// - Application services (analysis, validation, agent execution)
    /// - Infrastructure adapters with caching decorators
    /// - Orchestration layer components
    /// - CLI command handlers
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AnalyzeCodeCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(RunTestsCommand).Assembly);
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(AnalyzeCodeValidator).Assembly);

        // Register validation pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Nexo.Core.Application.Behaviors.ValidationBehavior<,>));

        // Register configuration service
        services.AddSingleton<Nexo.Core.Application.Configuration.Ports.IConfigurationService, Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter>();

        // Register orchestration layer
        services.AddNexoOrchestration();

        // Register IModel (using EchoModel for now, replace with real LLM adapter later)
        // Note: EchoModel is in Nexo.Adapters.Models which may not be referenced
        // For now, we'll skip this registration if the assembly isn't available
        try
        {
            var echoModelType = Type.GetType("Nexo.Adapters.Models.EchoModel, Nexo.Adapters.Models");
            if (echoModelType != null)
            {
                services.AddSingleton(typeof(Nexo.Abstractions.IModel), echoModelType);
            }
        }
        catch
        {
            // EchoModel not available, skip registration
        }
        
        // Register IProviderFactory for agent execution
        services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory, Nexo.Infrastructure.Execution.ProviderFactory>();

        // Register CLI commands
        services.AddScoped<AnalyzeCommand>();
        services.AddScoped<ValidateCommand>();
        services.AddScoped<AgentCommand>();
        services.AddScoped<ListAgentsCommand>();
        services.AddScoped<ConfigCommand>();
        // services.AddScoped<TestCommand>(); // TODO: Re-implement test runner command
        services.AddScoped<OrchestrateCommand>();
        services.AddScoped<EscalateCommand>();
        services.AddScoped<MetricsCommand>();
        services.AddScoped<UnityCommand>();
        services.AddScoped<DemoCommand>();

        // Register test runner
        services.AddScoped<Nexo.Core.Application.Testing.Ports.ITestRunner, Nexo.Infrastructure.Testing.TestRunnerAdapter>();

        // Register renderer
        services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();

        // Register test result parsers
        services.AddScoped<Nexo.Infrastructure.Validation.Parsers.ITestResultParser, Nexo.Infrastructure.Validation.Parsers.TrxTestResultParser>();

        // Register analysis rules
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule, Nexo.Infrastructure.Analysis.Rules.SecurityAnalysisRule>();
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule, Nexo.Infrastructure.Analysis.Rules.CodeQualityRule>();

        // Register analysis rule engine
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>(sp =>
        {
            var rules = sp.GetServices<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>>();
            return new Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine(rules, logger);
        });

        // Register caching
        services.AddSingleton<Nexo.Core.Application.Common.Ports.ICacheStrategy, Nexo.Infrastructure.Caching.MemoryCacheStrategy>();

        // Register metrics
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IMetricsCollector, Nexo.Infrastructure.Metrics.MemoryMetricsCollector>();

        // Register agent registry
        services.AddScoped<Nexo.Core.Application.Agent.Ports.IAgentRegistry, Nexo.Infrastructure.Agent.Adapters.AgentRegistryAdapter>();

        // Register infrastructure adapters (with caching decorators)
        services.AddScoped<Nexo.Core.Application.Analysis.Ports.IAnalysisService>(sp =>
        {
            var inner = new Nexo.Infrastructure.Analysis.Adapters.AnalysisServiceAdapter(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Adapters.AnalysisServiceAdapter>>(),
                sp.GetRequiredService<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>());
            var cache = sp.GetRequiredService<Nexo.Core.Application.Common.Ports.ICacheStrategy>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Adapters.CachedAnalysisServiceAdapter>>();
            return new Nexo.Infrastructure.Analysis.Adapters.CachedAnalysisServiceAdapter(inner, cache, logger);
        });

        services.AddScoped<Nexo.Core.Application.Validation.Ports.IValidationService>(sp =>
        {
            var inner = new Nexo.Infrastructure.Validation.Adapters.ValidationServiceAdapter(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Validation.Adapters.ValidationServiceAdapter>>(),
                sp.GetRequiredService<Nexo.Infrastructure.Validation.Parsers.ITestResultParser>());
            var cache = sp.GetRequiredService<Nexo.Core.Application.Common.Ports.ICacheStrategy>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Validation.Adapters.CachedValidationServiceAdapter>>();
            return new Nexo.Infrastructure.Validation.Adapters.CachedValidationServiceAdapter(inner, cache, logger);
        });

        services.AddScoped<Nexo.Core.Application.Agent.Ports.IAgentExecutor, Nexo.Infrastructure.Agent.Adapters.AgentExecutorAdapter>();

        // Register available agents
    }
}
