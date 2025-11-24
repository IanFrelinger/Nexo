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
using Nexo.Abstractions;
using Nexo.Demo.CLI.Agents;
using Nexo.Agents.Dev;

namespace Nexo.CLI;

static class Program
{
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

        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);
        root.AddCommand(configCmd);

        return await root.InvokeAsync(args);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AnalyzeCodeCommand).Assembly);
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(AnalyzeCodeValidator).Assembly);

        // Register validation pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Nexo.Core.Application.Behaviors.ValidationBehavior<,>));

        // Register configuration service
        services.AddSingleton<Nexo.Core.Application.Configuration.Ports.IConfigurationService, Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter>();

        // Register CLI commands
        services.AddScoped<AnalyzeCommand>();
        services.AddScoped<ValidateCommand>();
        services.AddScoped<AgentCommand>();
        services.AddScoped<ListAgentsCommand>();
        services.AddScoped<ConfigCommand>();

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
        services.AddTransient<IAgent, DirectorAgent>(sp => new DirectorAgent("director"));
        services.AddTransient<IAgent, DevDirectorAgent>(sp => new DevDirectorAgent(DevMode.Heal));
    }
}
