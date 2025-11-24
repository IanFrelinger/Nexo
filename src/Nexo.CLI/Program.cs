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
            async (DirectoryInfo path, bool json) =>
            {
                var exitCode = await analyzeCommand.ExecuteAsync(path, json);
                Environment.Exit(exitCode);
            },
            analyzeCmd.Options[0] as Option<DirectoryInfo> ?? throw new InvalidOperationException(),
            jsonOpt);

        // nexo validate
        var validateCmd = new Command("validate", "Run architecture tests/contract checks quickly")
        {
            new Option<string?>("--filter", "Optional test filter (Category/Trait)")
        };
        validateCmd.SetHandler(
            async (string? filter, bool json) =>
            {
                var exitCode = await validateCommand.ExecuteAsync(filter, json);
                Environment.Exit(exitCode);
            },
            validateCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt);

        // nexo agent
        var agentCmd = new Command("agent", "Run an agent action")
        {
            new Option<string>("--name", "Agent name") { IsRequired = true },
            new Option<FileInfo?>("--input", "Optional input file")
        };
        agentCmd.SetHandler(
            async (string name, FileInfo? input, bool json) =>
            {
                var exitCode = await agentCommand.ExecuteAsync(name, input, json);
                Environment.Exit(exitCode);
            },
            agentCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            agentCmd.Options[1] as Option<FileInfo?> ?? throw new InvalidOperationException(),
            jsonOpt);

        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);

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

        // Register CLI commands
        services.AddScoped<AnalyzeCommand>();
        services.AddScoped<ValidateCommand>();
        services.AddScoped<AgentCommand>();

        // Register renderer
        services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();

        // Register infrastructure adapters
        services.AddScoped<Nexo.Core.Application.Analysis.Ports.IAnalysisService, Nexo.Infrastructure.Analysis.Adapters.AnalysisServiceAdapter>();
        services.AddScoped<Nexo.Core.Application.Validation.Ports.IValidationService, Nexo.Infrastructure.Validation.Adapters.ValidationServiceAdapter>();
        services.AddScoped<Nexo.Core.Application.Agent.Ports.IAgentExecutor, Nexo.Infrastructure.Agent.Adapters.AgentExecutorAdapter>();

        // Register available agents
        services.AddTransient<IAgent, DirectorAgent>(sp => new DirectorAgent("director"));
        services.AddTransient<IAgent, DevDirectorAgent>(sp => new DevDirectorAgent(DevMode.Heal));
    }
}
