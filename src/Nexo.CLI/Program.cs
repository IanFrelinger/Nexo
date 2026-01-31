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
using Nexo.Orchestration;
using Nexo.Orchestration.Models;
using Nexo.Infrastructure.Persistence;

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
        var geoTerrainCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.GeoTerrain.GeoTerrainCommand>();
        var geoVectorCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.GeoVector.GeoVectorCommand>();
        var worldCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.World.WorldCommand>();

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
        var configShowCmd = new Command("show", "Show current configuration");
        configShowCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configShowCmd);
        var configValidateCmd = new Command("validate", "Validate configuration");
        configValidateCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ValidateAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configValidateCmd);
        var configExportPathOpt = new Option<FileInfo>("--path", "Output file path") { IsRequired = true };
        var configExportCmd = new Command("export", "Export configuration to file") { configExportPathOpt };
        configExportCmd.SetHandler(
            async (FileInfo path, bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ExportAsync(path.FullName, json, verbose);
                Environment.Exit(exitCode);
            },
            configExportPathOpt,
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configExportCmd);
        var configImportPathOpt = new Option<FileInfo>("--path", "Input file path") { IsRequired = true };
        var configImportCmd = new Command("import", "Import and validate configuration from file") { configImportPathOpt };
        configImportCmd.SetHandler(
            async (FileInfo path, bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ImportAsync(path.FullName, json, verbose);
                Environment.Exit(exitCode);
            },
            configImportPathOpt,
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configImportCmd);
        configCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var exitCode = await configCommand.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);

        // nexo background-agent
        var backgroundAgentCommand = serviceProvider.GetRequiredService<BackgroundAgentCommand>();
        var backgroundAgentCmd = new Command("background-agent", "Configure and manage background agents");
        var listBgCmd = new Command("list", "List configured background agents")
        {
            new Option<string?>("--status", "Filter by status (Running, Stopped, Error, NotRegistered)"),
            new Option<string?>("--role", "Filter by role"),
            new Option<string?>("--sensitivity", "Filter by max sensitivity level")
        };
        listBgCmd.SetHandler(
            async (bool formatJson, string? status, string? role, string? sensitivity) =>
            {
                var exitCode = await backgroundAgentCommand.ListAsync(formatJson, status, role, sensitivity);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            listBgCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            listBgCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            listBgCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException());
        backgroundAgentCmd.AddCommand(listBgCmd);

        var showBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var showBgCmd = new Command("show", "Show details for a background agent") { showBgIdOpt };
        showBgCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var exitCode = await backgroundAgentCommand.ShowAsync(id, formatJson);
                Environment.Exit(exitCode);
            },
            showBgIdOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(showBgCmd);

        var startBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var startBgCmd = new Command("start", "Start a background agent") { startBgIdOpt };
        startBgCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var exitCode = await backgroundAgentCommand.StartAsync(id, formatJson);
                Environment.Exit(exitCode);
            },
            startBgIdOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(startBgCmd);

        var stopBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var stopBgCmd = new Command("stop", "Stop a background agent") { stopBgIdOpt };
        stopBgCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var exitCode = await backgroundAgentCommand.StopAsync(id, formatJson);
                Environment.Exit(exitCode);
            },
            stopBgIdOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(stopBgCmd);

        var restartBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var restartBgCmd = new Command("restart", "Restart a background agent") { restartBgIdOpt };
        restartBgCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var exitCode = await backgroundAgentCommand.RestartAsync(id, formatJson);
                Environment.Exit(exitCode);
            },
            restartBgIdOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(restartBgCmd);

        // nexo background-agent execute
        var executeBgCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ExecuteBackgroundAgentCommand>();
        var executeBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var executeBgAsyncOpt = new Option<bool>("--async", "Run execution asynchronously (don't wait)");
        var executeBgCmd = new Command("execute", "Manually run one execution of a background agent") { executeBgIdOpt, executeBgAsyncOpt };
        executeBgCmd.SetHandler(
            async (string id, bool runAsync, bool formatJson) =>
                Environment.Exit(await executeBgCommand.ExecuteAsync(id, runAsync, formatJson)),
            executeBgIdOpt, executeBgAsyncOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(executeBgCmd);

        // nexo background-agent logs
        var logsBgCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.LogsBackgroundAgentCommand>();
        var logsBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var logsBgCmd = new Command("logs", "Show agent execution logs")
        {
            logsBgIdOpt,
            new Option<int>("--tail", () => 100, "Show last N lines"),
            new Option<string?>("--level", "Filter by level (Debug, Info, Warning, Error)"),
            new Option<string?>("--since", "Show logs since duration (e.g. 1h, 30m)")
        };
        logsBgCmd.SetHandler(
            async (string id, int tail, string? level, string? since, bool formatJson) =>
            {
                TimeSpan? sinceTs = ParseSince(since);
                Environment.Exit(await logsBgCommand.ExecuteAsync(id, tail, level, sinceTs, formatJson));
            },
            logsBgIdOpt,
            logsBgCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            logsBgCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            logsBgCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt);
        backgroundAgentCmd.AddCommand(logsBgCmd);

        // nexo background-agent metrics
        var metricsBgCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.MetricsBackgroundAgentCommand>();
        var metricsBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var metricsBgCmd = new Command("metrics", "Show agent performance metrics") { metricsBgIdOpt };
        metricsBgCmd.SetHandler(
            async (string id, bool formatJson) =>
                Environment.Exit(await metricsBgCommand.ExecuteAsync(id, formatJson)),
            metricsBgIdOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(metricsBgCmd);

        // nexo background-agent sensitivity
        var sensitivityCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
        var sensitivityCmd = new Command("sensitivity", "Manage data sensitivity levels");
        var sensListCmd = new Command("list", "List sensitivity levels");
        sensListCmd.SetHandler(async (bool formatJson) => Environment.Exit(await sensitivityCommand.ListAsync(formatJson)), jsonOpt);
        sensitivityCmd.AddCommand(sensListCmd);
        var sensShowNameOpt = new Option<string>("--name", "Sensitivity level name") { IsRequired = true };
        var sensShowCmd = new Command("show", "Show a sensitivity level") { sensShowNameOpt };
        sensShowCmd.SetHandler(async (string name, bool formatJson) => Environment.Exit(await sensitivityCommand.ShowAsync(name, formatJson)), sensShowNameOpt, jsonOpt);
        sensitivityCmd.AddCommand(sensShowCmd);
        var sensAddNameOpt = new Option<string>("--name", "Level name") { IsRequired = true };
        var sensAddValueOpt = new Option<int>("--value", () => 0, "Sensitivity value (ordering)");
        var sensAddDescOpt = new Option<string>("--description", "Description");
        var sensAddCmd = new Command("add", "Add a custom sensitivity level")
        {
            sensAddNameOpt, sensAddValueOpt,
            new Option<bool>("--allows-external-llm", () => false),
            new Option<bool>("--allows-web-search", () => false),
            new Option<bool>("--requires-local-only", () => false),
            new Option<bool>("--allows-network-exports", () => false),
            sensAddDescOpt
        };
        sensAddCmd.SetHandler(
            async (string name, int value, bool allowsExternalLLM, bool allowsWebSearch, bool requiresLocalOnly, bool allowsNetworkExports, string? description, bool formatJson) =>
                Environment.Exit(await sensitivityCommand.AddAsync(name, value, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? "", formatJson)),
            sensAddNameOpt, sensAddValueOpt,
            sensAddCmd.Options[2] as Option<bool> ?? throw new InvalidOperationException(),
            sensAddCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException(),
            sensAddCmd.Options[4] as Option<bool> ?? throw new InvalidOperationException(),
            sensAddCmd.Options[5] as Option<bool> ?? throw new InvalidOperationException(),
            sensAddDescOpt, jsonOpt);
        sensitivityCmd.AddCommand(sensAddCmd);
        var sensUpdateNameOpt = new Option<string>("--name", "Level name") { IsRequired = true };
        var sensUpdateCmd = new Command("update", "Update a custom sensitivity level")
        {
            sensUpdateNameOpt, new Option<int>("--value", () => 0, "Sensitivity value"),
            new Option<bool>("--allows-external-llm", () => false),
            new Option<bool>("--allows-web-search", () => false),
            new Option<bool>("--requires-local-only", () => false),
            new Option<bool>("--allows-network-exports", () => false),
            new Option<string>("--description", "Description")
        };
        sensUpdateCmd.SetHandler(
            async (string name, int value, bool allowsExternalLLM, bool allowsWebSearch, bool requiresLocalOnly, bool allowsNetworkExports, string? description, bool formatJson) =>
                Environment.Exit(await sensitivityCommand.UpdateAsync(name, value, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? "", formatJson)),
            sensUpdateNameOpt, sensUpdateCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            sensUpdateCmd.Options[2] as Option<bool> ?? throw new InvalidOperationException(),
            sensUpdateCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException(),
            sensUpdateCmd.Options[4] as Option<bool> ?? throw new InvalidOperationException(),
            sensUpdateCmd.Options[5] as Option<bool> ?? throw new InvalidOperationException(),
            sensUpdateCmd.Options[6] as Option<string> ?? throw new InvalidOperationException(),
            jsonOpt);
        sensitivityCmd.AddCommand(sensUpdateCmd);
        var sensRemoveNameOpt = new Option<string>("--name", "Level name") { IsRequired = true };
        var sensRemoveCmd = new Command("remove", "Remove a custom sensitivity level") { sensRemoveNameOpt };
        sensRemoveCmd.SetHandler(async (string name, bool formatJson) => Environment.Exit(await sensitivityCommand.RemoveAsync(name, formatJson)), sensRemoveNameOpt, jsonOpt);
        sensitivityCmd.AddCommand(sensRemoveCmd);
        backgroundAgentCmd.AddCommand(sensitivityCmd);

        // nexo background-agent rag
        var ragCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
        var ragCmd = new Command("rag", "RAG (Retrieval Augmented Generation) operations");
        var ragIndexPathsOpt = new Option<string[]>("--paths", "Paths to index (files or directories)") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        var ragIndexSensOpt = new Option<string?>("--sensitivity", "Default sensitivity level for indexed documents");
        var ragIndexCmd = new Command("index", "Index paths into RAG store") { ragIndexPathsOpt, ragIndexSensOpt };
        ragIndexCmd.SetHandler(
            async (string[] paths, string? sensitivity, bool formatJson) =>
                Environment.Exit(await ragCommand.IndexAsync(paths ?? Array.Empty<string>(), sensitivity, formatJson)),
            ragIndexPathsOpt, ragIndexSensOpt, jsonOpt);
        ragCmd.AddCommand(ragIndexCmd);
        var ragSearchQueryOpt = new Option<string>("--query", "Search query") { IsRequired = true };
        var ragSearchCmd = new Command("search", "Search RAG store")
        {
            ragSearchQueryOpt,
            new Option<int>("--max-results", () => 5, "Max results"),
            new Option<double>("--min-score", () => 0.0, "Min similarity score"),
            new Option<string?>("--max-sensitivity", "Max sensitivity level for results")
        };
        ragSearchCmd.SetHandler(
            async (string query, int maxResults, double minScore, string? maxSensitivity, bool formatJson) =>
                Environment.Exit(await ragCommand.SearchAsync(query, maxResults, minScore, maxSensitivity, formatJson)),
            ragSearchQueryOpt,
            ragSearchCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            ragSearchCmd.Options[2] as Option<double> ?? throw new InvalidOperationException(),
            ragSearchCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt);
        ragCmd.AddCommand(ragSearchCmd);
        var ragStatsCmd = new Command("stats", "Show RAG store statistics");
        ragStatsCmd.SetHandler(async (bool formatJson) => Environment.Exit(await ragCommand.StatsAsync(formatJson)), jsonOpt);
        ragCmd.AddCommand(ragStatsCmd);
        var ragClearCmd = new Command("clear", "Clear RAG store");
        ragClearCmd.SetHandler(async (bool formatJson) => Environment.Exit(await ragCommand.ClearAsync(formatJson)), jsonOpt);
        ragCmd.AddCommand(ragClearCmd);
        backgroundAgentCmd.AddCommand(ragCmd);

        // nexo background-agent websearch
        var webSearchCommand = serviceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.WebSearchCommand>();
        var webSearchCmd = new Command("websearch", "Web search configuration and test");
        var webSearchConfigureCmd = new Command("configure", "Show web search configuration");
        webSearchConfigureCmd.SetHandler(async (bool formatJson) => Environment.Exit(await webSearchCommand.ConfigureAsync(formatJson)), jsonOpt);
        webSearchCmd.AddCommand(webSearchConfigureCmd);
        var webSearchTestCmd = new Command("test", "Run a test search")
        {
            new Option<string>("--query", () => "Nexo framework", "Search query"),
            new Option<int>("--max-results", () => 5, "Max results")
        };
        webSearchTestCmd.SetHandler(
            async (string query, int maxResults, bool formatJson) =>
                Environment.Exit(await webSearchCommand.TestAsync(query, maxResults, formatJson)),
            webSearchTestCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            webSearchTestCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            jsonOpt);
        webSearchCmd.AddCommand(webSearchTestCmd);
        backgroundAgentCmd.AddCommand(webSearchCmd);

        // nexo test - Multi-platform test execution
        var testCmd = new Command("test", "Run tests across multiple platforms")
        {
            new Option<string[]>("--platforms", "Platforms to test (ubuntu, alpine, debian, android, ios, unity, windows, macos)")
            {
                AllowMultipleArgumentsPerToken = true
            },
            new Option<string?>("--project", "Test project to run"),
            new Option<string?>("--filter", "Test filter (xUnit filter syntax)"),
            new Option<string>("--dotnet-version", () => "8.0", ".NET version to use"),
            new Option<string>("--execution-platform", () => "docker", "Execution platform (docker, rancher, kubernetes)"),
            new Option<DirectoryInfo>("--output-dir", () => new DirectoryInfo("test-results"), "Directory for test results")
        };
        var platformsOpt = testCmd.Options[0] as Option<string[]> ?? throw new InvalidOperationException();
        var projectOpt = testCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException();
        var filterOpt = testCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException();
        var dotnetVersionOpt = testCmd.Options[3] as Option<string> ?? throw new InvalidOperationException();
        var executionPlatformOpt = testCmd.Options[4] as Option<string> ?? throw new InvalidOperationException();
        var outputDirOpt = testCmd.Options[5] as Option<DirectoryInfo> ?? throw new InvalidOperationException();
        
        var coverageOpt = new Option<bool>("--coverage", () => false, "Enable code coverage collection");
        var stressOpt = new Option<bool>("--stress", () => false, "Run stress tests (multiple iterations)");
        var visualOpt = new Option<bool>("--visual", () => false, "Run visual validation tests (requires Ollama)");
        testCmd.AddOption(coverageOpt);
        testCmd.AddOption(stressOpt);
        testCmd.AddOption(visualOpt);

        testCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var platforms = ctx.ParseResult.GetValueForOption(platformsOpt) ?? Array.Empty<string>();
            var project = ctx.ParseResult.GetValueForOption(projectOpt);
            var filter = ctx.ParseResult.GetValueForOption(filterOpt);
            var dotnetVersion = ctx.ParseResult.GetValueForOption(dotnetVersionOpt) ?? "8.0";
            var executionPlatform = ctx.ParseResult.GetValueForOption(executionPlatformOpt) ?? "docker";
            var outputDir = ctx.ParseResult.GetValueForOption(outputDirOpt) ?? new DirectoryInfo("test-results");
            var coverage = ctx.ParseResult.GetValueForOption(coverageOpt);
            var stress = ctx.ParseResult.GetValueForOption(stressOpt);
            var visual = ctx.ParseResult.GetValueForOption(visualOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            
            var exitCode = await MultiPlatformTestCommand.ExecuteAsync(
                platforms, project, filter, dotnetVersion, executionPlatform, outputDir, coverage, stress, visual, json, verbose, serviceProvider);
            Environment.Exit(exitCode);
        });
        // nexo test local - Run tests locally (replaces test-local.sh)
        var testLocalCmd = new Command("local", "Run tests locally using framework test runner")
        {
            new Option<string?>("--filter", "Filter tests by name or category")
        };
        testLocalCmd.SetHandler(
            async (string? filter, bool json, bool verbose) =>
            {
                var testCommand = serviceProvider.GetRequiredService<TestCommand>();
                var exitCode = await testCommand.ExecuteAsync(filter, json, verbose);
                Environment.Exit(exitCode);
            },
            testLocalCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        testCmd.AddCommand(testLocalCmd);
        testCmd.AddCommand(TestPortableCommand.CreateCommand());
        testCmd.AddCommand(TestMultiEnvCommand.CreateCommand());

        // nexo orchestrate
        var orchestrateCommand = serviceProvider.GetService<OrchestrateCommand>();
        if (orchestrateCommand != null)
        {
            var orchestrateCmd = new Command("orchestrate", "Orchestrate agent execution for a request");
            orchestrateCmd.AddArgument(new Argument<string>("request", "The request to orchestrate"));

            var runtimeSpecOpt = new Option<FileInfo?>(
                name: "--runtime-spec",
                description: "Runtime spec JSON file (model routing per domain/agent)");
            var runtimeSpecJsonOpt = new Option<string?>(
                name: "--runtime-spec-json",
                description: "Runtime spec JSON string (model routing per domain/agent)");
            var preferModelOpt = new Option<string?>(
                name: "--prefer-model",
                description: "Override model preference: agentic|deterministic|auto");
            var providerOpt = new Option<string?>(
                name: "--provider",
                description: "Override model provider (openai/azure/ollama/offline/mock-json/...)");

            orchestrateCmd.AddOption(runtimeSpecOpt);
            orchestrateCmd.AddOption(runtimeSpecJsonOpt);
            orchestrateCmd.AddOption(preferModelOpt);
            orchestrateCmd.AddOption(providerOpt);

            orchestrateCmd.SetHandler(
                async (string request, FileInfo? runtimeSpec, string? runtimeSpecJson, string? preferModel, string? provider, bool json, bool verbose) =>
                {
                    var exitCode = await orchestrateCommand.ExecuteAsync(
                        request,
                        runtimeSpec?.FullName,
                        runtimeSpecJson,
                        preferModel,
                        provider,
                        json,
                        verbose);
                    Environment.Exit(exitCode);
                },
                orchestrateCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
                runtimeSpecOpt,
                runtimeSpecJsonOpt,
                preferModelOpt,
                providerOpt,
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

        // nexo geoterrain
        var geoCmd = new Command("geoterrain", "GeoTerrain GIS elevation → mesh utilities");
        var tileToObjCmd = new Command("tile-to-obj", "Fetch an SRTM tile and write an OBJ mesh");
        var terrainRgbTileToObjCmd = new Command("terrain-rgb-tile-to-obj", "Download a Mapbox Terrain-RGB tile and write an OBJ (optionally also the tile PNG)");
        var mapboxRasterTileCmd = new Command("mapbox-raster-tile", "Download a Mapbox raster tile (e.g. satellite) to disk");
        var tileToContoursCmd = new Command("tile-to-contours", "Fetch an SRTM tile and write contour lines as GeoJSON");
        var boundsToObjCmd = new Command("bounds-to-obj", "Fetch all tiles covering bounds and write a stitched OBJ mesh");
        var boundsToContoursCmd = new Command("bounds-to-contours", "Fetch all tiles covering bounds and write stitched contours as GeoJSON");
        var boundsToTreesCmd = new Command("bounds-to-tree-instances", "Fetch tiles covering bounds and write tree instance placements as JSON");
        var tileOpt = new Option<string>("--tile", "Tile id like N00E000 or N00E000.hgt") { IsRequired = true };
        var geoTerrainBoundsOpt = new Option<string>("--bounds", "Bounds: minLat,minLon,maxLat,maxLon") { IsRequired = true };
        var outOpt = new Option<FileInfo>("--output", "Output OBJ file path") { IsRequired = true };
        var elevationProviderOpt = new Option<string>("--elevation-provider", () => "echo", "Provider: echo|local|http|hybrid");
        var localRootOpt = new Option<string?>("--local-root", () => null, "Local SRTM cache directory (for local/hybrid providers)");
        var baseUrlOpt = new Option<string?>("--srtm-base-url", () => null, "Base URL for HTTP downloads (for http/hybrid providers)");
        var persistOpt = new Option<bool>("--persist-downloads", () => true, "Persist downloaded tiles into --local-root (hybrid)");
        var cacheRootOpt = new Option<string?>("--cache-root", "Directory root for tile cache (saves downloaded tiles to disk)");
        var persistCacheOpt = new Option<bool>("--persist-cache", () => true, "Enable disk caching (default: true)");
        var cacheOpt = new Option<bool>("--cache", () => true, "Enable in-memory provider cache");
        var airgapOpt = new Option<bool>("--airgap", () => false, "Air-gapped mode: forces deterministic + disables network providers");
        var forceAgenticFailOpt = new Option<bool>("--force-agentic-fail", () => false, "Force agentic implementation to fail (to demonstrate fallback)");
        var intervalMetersOpt = new Option<double>("--interval-meters", () => 10.0, "Contour interval in meters");
        var minElevOpt = new Option<double?>("--min-elevation-meters", () => null, "Optional min contour level (meters)");
        var maxElevOpt = new Option<double?>("--max-elevation-meters", () => null, "Optional max contour level (meters)");
        var verticalScaleOpt = new Option<float>("--vertical-scale", () => 1.0f, "Vertical scale multiplier");
        var treatNoDataOpt = new Option<bool>("--treat-nodata-as-zero", () => false, "If true, NaN becomes 0m");
        var includeElevationOpt = new Option<bool>("--include-elevation", () => true, "Include elevation as the 3rd coordinate in GeoJSON");
        var treesPerSqKmOpt = new Option<float>("--trees-per-sqkm", () => 200.0f, "Tree density (trees per square kilometer)");
        var treeSeedOpt = new Option<int>("--seed", () => 1337, "Deterministic seed for placement");
        var validateIntegrityOpt = new Option<bool>("--validate-integrity", () => false, "Validate data integrity (checksum, corruption detection)");
        var meshQualityReportOpt = new Option<bool>("--mesh-quality-report", () => false, "Generate mesh quality metrics report");

        var zOpt = new Option<int>("--z", "WebMercator tile zoom") { IsRequired = true };
        var xOpt = new Option<int>("--x", "WebMercator tile x") { IsRequired = true };
        var yOpt = new Option<int>("--y", "WebMercator tile y") { IsRequired = true };
        var textureOutOpt = new Option<FileInfo?>("--texture-out", () => null, "Optional output PNG path for the downloaded tile");
        var mapboxTokenOpt2 = new Option<string?>("--mapbox-token", () => null, "Mapbox access token (or set MAPBOX_ACCESS_TOKEN)");
        var mapboxTilesetOpt2 = new Option<string?>("--mapbox-tileset", () => "mapbox.terrain-rgb", "Tileset id (default mapbox.terrain-rgb)");
        var mapboxFormatOpt = new Option<string?>("--format", () => null, "Tile format/extension for v4 endpoint (e.g. png, jpg90)");
        var mapboxRasterTilesetOpt = new Option<string?>("--mapbox-tileset", () => "mapbox.satellite", "Raster tileset id (default mapbox.satellite)");

        tileToObjCmd.AddOption(tileOpt);
        tileToObjCmd.AddOption(outOpt);
        tileToObjCmd.AddOption(elevationProviderOpt);
        tileToObjCmd.AddOption(localRootOpt);
        tileToObjCmd.AddOption(baseUrlOpt);
        tileToObjCmd.AddOption(persistOpt);
        tileToObjCmd.AddOption(cacheOpt);
        tileToObjCmd.AddOption(cacheRootOpt);
        tileToObjCmd.AddOption(persistCacheOpt);
        tileToObjCmd.AddOption(airgapOpt);
        tileToObjCmd.AddOption(forceAgenticFailOpt);
        tileToObjCmd.AddOption(validateIntegrityOpt);
        tileToObjCmd.AddOption(meshQualityReportOpt);

        tileToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var tile = ctx.ParseResult.GetValueForOption(tileOpt) ?? throw new InvalidOperationException("--tile is required");
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var elevationProvider = ctx.ParseResult.GetValueForOption(elevationProviderOpt) ?? "echo";
            var localRoot = ctx.ParseResult.GetValueForOption(localRootOpt);
            var srtmBaseUrl = ctx.ParseResult.GetValueForOption(baseUrlOpt);
            var persistDownloads = ctx.ParseResult.GetValueForOption(persistOpt);
            var cache = ctx.ParseResult.GetValueForOption(cacheOpt);
            var cacheRoot = ctx.ParseResult.GetValueForOption(cacheRootOpt);
            var persistCache = ctx.ParseResult.GetValueForOption(persistCacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(airgapOpt);
            var forceAgenticFail = ctx.ParseResult.GetValueForOption(forceAgenticFailOpt);
            var validateIntegrity = ctx.ParseResult.GetValueForOption(validateIntegrityOpt);
            var meshQualityReport = ctx.ParseResult.GetValueForOption(meshQualityReportOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.TileToObjAsync(
                tile,
                output,
                elevationProvider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                cache,
                airgap,
                forceAgenticFail,
                validateIntegrity,
                meshQualityReport,
                cacheRoot,
                persistCache,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        geoCmd.AddCommand(tileToObjCmd);

        // geoterrain terrain-rgb-tile-to-obj
        terrainRgbTileToObjCmd.AddOption(zOpt);
        terrainRgbTileToObjCmd.AddOption(xOpt);
        terrainRgbTileToObjCmd.AddOption(yOpt);
        terrainRgbTileToObjCmd.AddOption(outOpt);
        terrainRgbTileToObjCmd.AddOption(textureOutOpt);
        terrainRgbTileToObjCmd.AddOption(mapboxTokenOpt2);
        terrainRgbTileToObjCmd.AddOption(mapboxTilesetOpt2);
        terrainRgbTileToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var z = ctx.ParseResult.GetValueForOption(zOpt);
            var x = ctx.ParseResult.GetValueForOption(xOpt);
            var y = ctx.ParseResult.GetValueForOption(yOpt);
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var textureOut = ctx.ParseResult.GetValueForOption(textureOutOpt);
            var token = ctx.ParseResult.GetValueForOption(mapboxTokenOpt2);
            var tileset = ctx.ParseResult.GetValueForOption(mapboxTilesetOpt2);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.TerrainRgbTileToObjAsync(
                z,
                x,
                y,
                output,
                textureOut,
                token,
                tileset,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });
        geoCmd.AddCommand(terrainRgbTileToObjCmd);

        // geoterrain mapbox-raster-tile
        mapboxRasterTileCmd.AddOption(zOpt);
        mapboxRasterTileCmd.AddOption(xOpt);
        mapboxRasterTileCmd.AddOption(yOpt);
        mapboxRasterTileCmd.AddOption(outOpt);
        mapboxRasterTileCmd.AddOption(mapboxTokenOpt2);
        mapboxRasterTileCmd.AddOption(mapboxRasterTilesetOpt);
        mapboxRasterTileCmd.AddOption(mapboxFormatOpt);
        mapboxRasterTileCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var z = ctx.ParseResult.GetValueForOption(zOpt);
            var x = ctx.ParseResult.GetValueForOption(xOpt);
            var y = ctx.ParseResult.GetValueForOption(yOpt);
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var token = ctx.ParseResult.GetValueForOption(mapboxTokenOpt2);
            var tileset = ctx.ParseResult.GetValueForOption(mapboxRasterTilesetOpt);
            var format = ctx.ParseResult.GetValueForOption(mapboxFormatOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.MapboxRasterTileAsync(z, x, y, output, token, tileset, format, json, verbose, CancellationToken.None);
            ctx.ExitCode = exitCode;
        });
        geoCmd.AddCommand(mapboxRasterTileCmd);

        // geoterrain tile-to-contours
        tileToContoursCmd.AddOption(tileOpt);
        tileToContoursCmd.AddOption(outOpt);
        tileToContoursCmd.AddOption(elevationProviderOpt);
        tileToContoursCmd.AddOption(localRootOpt);
        tileToContoursCmd.AddOption(baseUrlOpt);
        tileToContoursCmd.AddOption(persistOpt);
        tileToContoursCmd.AddOption(cacheOpt);
        tileToContoursCmd.AddOption(airgapOpt);
        tileToContoursCmd.AddOption(forceAgenticFailOpt);
        tileToContoursCmd.AddOption(intervalMetersOpt);
        tileToContoursCmd.AddOption(minElevOpt);
        tileToContoursCmd.AddOption(maxElevOpt);
        tileToContoursCmd.AddOption(verticalScaleOpt);
        tileToContoursCmd.AddOption(treatNoDataOpt);
        tileToContoursCmd.AddOption(includeElevationOpt);

        tileToContoursCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var tile = ctx.ParseResult.GetValueForOption(tileOpt) ?? throw new InvalidOperationException("--tile is required");
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var elevationProvider = ctx.ParseResult.GetValueForOption(elevationProviderOpt) ?? "echo";
            var localRoot = ctx.ParseResult.GetValueForOption(localRootOpt);
            var srtmBaseUrl = ctx.ParseResult.GetValueForOption(baseUrlOpt);
            var persistDownloads = ctx.ParseResult.GetValueForOption(persistOpt);
            var cache = ctx.ParseResult.GetValueForOption(cacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(airgapOpt);
            var forceAgenticFail = ctx.ParseResult.GetValueForOption(forceAgenticFailOpt);
            var intervalMeters = ctx.ParseResult.GetValueForOption(intervalMetersOpt);
            var minElev = ctx.ParseResult.GetValueForOption(minElevOpt);
            var maxElev = ctx.ParseResult.GetValueForOption(maxElevOpt);
            var verticalScale = ctx.ParseResult.GetValueForOption(verticalScaleOpt);
            var treatNoData = ctx.ParseResult.GetValueForOption(treatNoDataOpt);
            var includeElevation = ctx.ParseResult.GetValueForOption(includeElevationOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.TileToContoursAsync(
                tile,
                output,
                elevationProvider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                cache,
                airgap,
                forceAgenticFail,
                intervalMeters,
                minElev,
                maxElev,
                verticalScale,
                treatNoData,
                includeElevation,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        geoCmd.AddCommand(tileToContoursCmd);

        // geoterrain bounds-to-obj
        boundsToObjCmd.AddOption(geoTerrainBoundsOpt);
        boundsToObjCmd.AddOption(outOpt);
        boundsToObjCmd.AddOption(elevationProviderOpt);
        boundsToObjCmd.AddOption(localRootOpt);
        boundsToObjCmd.AddOption(baseUrlOpt);
        boundsToObjCmd.AddOption(persistOpt);
        boundsToObjCmd.AddOption(cacheOpt);
        boundsToObjCmd.AddOption(cacheRootOpt);
        boundsToObjCmd.AddOption(persistCacheOpt);
        boundsToObjCmd.AddOption(airgapOpt);
        boundsToObjCmd.AddOption(forceAgenticFailOpt);
        boundsToObjCmd.AddOption(validateIntegrityOpt);
        boundsToObjCmd.AddOption(meshQualityReportOpt);

        boundsToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(geoTerrainBoundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var elevationProvider = ctx.ParseResult.GetValueForOption(elevationProviderOpt) ?? "echo";
            var localRoot = ctx.ParseResult.GetValueForOption(localRootOpt);
            var srtmBaseUrl = ctx.ParseResult.GetValueForOption(baseUrlOpt);
            var persistDownloads = ctx.ParseResult.GetValueForOption(persistOpt);
            var cache = ctx.ParseResult.GetValueForOption(cacheOpt);
            var cacheRoot = ctx.ParseResult.GetValueForOption(cacheRootOpt);
            var persistCache = ctx.ParseResult.GetValueForOption(persistCacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(airgapOpt);
            var forceAgenticFail = ctx.ParseResult.GetValueForOption(forceAgenticFailOpt);
            var validateIntegrity = ctx.ParseResult.GetValueForOption(validateIntegrityOpt);
            var meshQualityReport = ctx.ParseResult.GetValueForOption(meshQualityReportOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.BoundsToObjAsync(
                bounds,
                output,
                elevationProvider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                cache,
                airgap,
                forceAgenticFail,
                validateIntegrity,
                meshQualityReport,
                cacheRoot,
                persistCache,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });
        geoCmd.AddCommand(boundsToObjCmd);

        // geoterrain bounds-to-contours
        boundsToContoursCmd.AddOption(geoTerrainBoundsOpt);
        boundsToContoursCmd.AddOption(outOpt);
        boundsToContoursCmd.AddOption(elevationProviderOpt);
        boundsToContoursCmd.AddOption(localRootOpt);
        boundsToContoursCmd.AddOption(baseUrlOpt);
        boundsToContoursCmd.AddOption(persistOpt);
        boundsToContoursCmd.AddOption(cacheOpt);
        boundsToContoursCmd.AddOption(airgapOpt);
        boundsToContoursCmd.AddOption(forceAgenticFailOpt);
        boundsToContoursCmd.AddOption(intervalMetersOpt);
        boundsToContoursCmd.AddOption(minElevOpt);
        boundsToContoursCmd.AddOption(maxElevOpt);
        boundsToContoursCmd.AddOption(verticalScaleOpt);
        boundsToContoursCmd.AddOption(treatNoDataOpt);
        boundsToContoursCmd.AddOption(includeElevationOpt);

        boundsToContoursCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(geoTerrainBoundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var elevationProvider = ctx.ParseResult.GetValueForOption(elevationProviderOpt) ?? "echo";
            var localRoot = ctx.ParseResult.GetValueForOption(localRootOpt);
            var srtmBaseUrl = ctx.ParseResult.GetValueForOption(baseUrlOpt);
            var persistDownloads = ctx.ParseResult.GetValueForOption(persistOpt);
            var cache = ctx.ParseResult.GetValueForOption(cacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(airgapOpt);
            var forceAgenticFail = ctx.ParseResult.GetValueForOption(forceAgenticFailOpt);
            var intervalMeters = ctx.ParseResult.GetValueForOption(intervalMetersOpt);
            var minElev = ctx.ParseResult.GetValueForOption(minElevOpt);
            var maxElev = ctx.ParseResult.GetValueForOption(maxElevOpt);
            var verticalScale = ctx.ParseResult.GetValueForOption(verticalScaleOpt);
            var treatNoData = ctx.ParseResult.GetValueForOption(treatNoDataOpt);
            var includeElevation = ctx.ParseResult.GetValueForOption(includeElevationOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.BoundsToContoursAsync(
                bounds,
                output,
                elevationProvider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                cache,
                airgap,
                forceAgenticFail,
                intervalMeters,
                minElev,
                maxElev,
                verticalScale,
                treatNoData,
                includeElevation,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });
        geoCmd.AddCommand(boundsToContoursCmd);

        // geoterrain bounds-to-tree-instances
        boundsToTreesCmd.AddOption(geoTerrainBoundsOpt);
        boundsToTreesCmd.AddOption(outOpt);
        boundsToTreesCmd.AddOption(elevationProviderOpt);
        boundsToTreesCmd.AddOption(localRootOpt);
        boundsToTreesCmd.AddOption(baseUrlOpt);
        boundsToTreesCmd.AddOption(persistOpt);
        boundsToTreesCmd.AddOption(cacheOpt);
        boundsToTreesCmd.AddOption(airgapOpt);
        boundsToTreesCmd.AddOption(treesPerSqKmOpt);
        boundsToTreesCmd.AddOption(treeSeedOpt);
        boundsToTreesCmd.AddOption(treatNoDataOpt);

        boundsToTreesCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(geoTerrainBoundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(outOpt) ?? throw new InvalidOperationException("--output is required");
            var elevationProvider = ctx.ParseResult.GetValueForOption(elevationProviderOpt) ?? "echo";
            var localRoot = ctx.ParseResult.GetValueForOption(localRootOpt);
            var srtmBaseUrl = ctx.ParseResult.GetValueForOption(baseUrlOpt);
            var persistDownloads = ctx.ParseResult.GetValueForOption(persistOpt);
            var cache = ctx.ParseResult.GetValueForOption(cacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(airgapOpt);
            var treesPerSqKm = ctx.ParseResult.GetValueForOption(treesPerSqKmOpt);
            var seed = ctx.ParseResult.GetValueForOption(treeSeedOpt);
            var treatNoData = ctx.ParseResult.GetValueForOption(treatNoDataOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoTerrainCommand.BoundsToTreeInstancesAsync(
                bounds,
                output,
                elevationProvider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                cache,
                airgap,
                treesPerSqKm,
                seed,
                treatNoData,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });
        geoCmd.AddCommand(boundsToTreesCmd);
        root.AddCommand(geoCmd);

        // nexo geovector
        var geoVectorCmd = new Command("geovector", "GeoVector overlays (buildings/roads/vegetation) → meshes");
        var buildingsToObjCmd = new Command("buildings-to-obj", "Fetch building footprints and write an OBJ mesh");
        var roadsToObjCmd = new Command("roads-to-obj", "Fetch road centerlines and write an OBJ ribbon mesh");
        var waterToObjCmd = new Command("water-to-obj", "Fetch water polygons and write an OBJ surface mesh");
        var boundsOpt = new Option<string>("--bounds", "Bounds: minLat,minLon,maxLat,maxLon") { IsRequired = true };
        var vecOutOpt = new Option<FileInfo>("--output", "Output OBJ file path") { IsRequired = true };
        var vectorProviderOpt = new Option<string>("--vector-provider", () => "echo", "Provider: echo|osm|mapbox|hybrid");
        var osmPbfOpt = new Option<string?>("--osm-pbf", () => null, "Path to local OSM .pbf extract (required for osm/hybrid)");
        var mapboxTokenOpt = new Option<string?>("--mapbox-token", () => null, "Mapbox access token (or set MAPBOX_ACCESS_TOKEN)");
        var mapboxTilesetOpt = new Option<string?>("--mapbox-tileset", () => "mapbox.mapbox-streets-v8", "Mapbox tileset id (e.g. mapbox.mapbox-streets-v8)");
        var mapboxZoomOpt = new Option<int?>("--mapbox-zoom", () => 15, "Zoom level for tile selection (0-22)");
        var generateUvOpt = new Option<bool>("--uv", () => false, "Generate UVs for consistent, meter-scaled textures");
        var uvMetersPerRepeatOpt = new Option<float>("--uv-meters-per-repeat", () => 1.0f, "Meters per texture repeat (UV scale)");
        var alignToTerrainOpt = new Option<bool>("--align-to-terrain", () => false, "Offset building bases to match terrain elevation");
        var terrainElevationProviderOpt = new Option<string>("--terrain-elevation-provider", () => "echo", "Terrain elevation provider: echo|local|http|hybrid");
        var terrainLocalRootOpt = new Option<string?>("--terrain-local-root", () => null, "Local SRTM cache directory (for terrain local/hybrid)");
        var terrainBaseUrlOpt = new Option<string?>("--terrain-srtm-base-url", () => null, "Base URL for terrain HTTP downloads (http/hybrid)");
        var terrainPersistOpt = new Option<bool>("--terrain-persist-downloads", () => true, "Persist downloaded terrain tiles into --terrain-local-root (hybrid)");
        var terrainCacheOpt = new Option<bool>("--terrain-cache", () => true, "Enable in-memory cache for terrain elevation provider");
        var terrainTreatNoDataOpt = new Option<bool>("--terrain-treat-nodata-as-zero", () => false, "If true, terrain NaN becomes 0m when aligning buildings");
        var vecCacheRootOpt = new Option<string?>("--cache-root", "Directory root for tile cache (saves downloaded tiles to disk)");
        var vecPersistCacheOpt = new Option<bool>("--persist-cache", () => true, "Enable disk caching (default: true)");
        var terrainCacheRootOpt = new Option<string?>("--terrain-cache-root", "Directory root for terrain tile cache");
        var terrainPersistCacheOpt = new Option<bool>("--terrain-persist-cache", () => true, "Enable terrain disk caching (default: true)");
        var vecAirgapOpt = new Option<bool>("--airgap", () => false, "Air-gapped mode: forces deterministic only");
        var vecForceFailOpt = new Option<bool>("--force-agentic-fail", () => false, "Force agentic implementation to fail (fallback demo)");

        var roadWidthOpt = new Option<float>("--width-meters", () => 4.0f, "Road width in meters");
        var conformToTerrainOpt = new Option<bool>("--conform-to-terrain", () => false, "Sample terrain height to conform mesh to terrain");

        var waterSurfaceOffsetOpt = new Option<float>("--surface-offset-meters", () => 0.0f, "Offset water surface when conforming to terrain");

        buildingsToObjCmd.AddOption(boundsOpt);
        buildingsToObjCmd.AddOption(vecOutOpt);
        buildingsToObjCmd.AddOption(vectorProviderOpt);
        buildingsToObjCmd.AddOption(osmPbfOpt);
        buildingsToObjCmd.AddOption(mapboxTokenOpt);
        buildingsToObjCmd.AddOption(mapboxTilesetOpt);
        buildingsToObjCmd.AddOption(mapboxZoomOpt);
        buildingsToObjCmd.AddOption(generateUvOpt);
        buildingsToObjCmd.AddOption(uvMetersPerRepeatOpt);
        buildingsToObjCmd.AddOption(alignToTerrainOpt);
        buildingsToObjCmd.AddOption(terrainElevationProviderOpt);
        buildingsToObjCmd.AddOption(terrainLocalRootOpt);
        buildingsToObjCmd.AddOption(terrainBaseUrlOpt);
        buildingsToObjCmd.AddOption(terrainPersistOpt);
        buildingsToObjCmd.AddOption(terrainCacheOpt);
        buildingsToObjCmd.AddOption(terrainTreatNoDataOpt);
        buildingsToObjCmd.AddOption(vecCacheRootOpt);
        buildingsToObjCmd.AddOption(vecPersistCacheOpt);
        buildingsToObjCmd.AddOption(terrainCacheRootOpt);
        buildingsToObjCmd.AddOption(terrainPersistCacheOpt);
        buildingsToObjCmd.AddOption(vecAirgapOpt);
        buildingsToObjCmd.AddOption(vecForceFailOpt);

        buildingsToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(boundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(vecOutOpt) ?? throw new InvalidOperationException("--output is required");
            var provider = ctx.ParseResult.GetValueForOption(vectorProviderOpt) ?? "echo";
            var osmPbf = ctx.ParseResult.GetValueForOption(osmPbfOpt);
            var mapboxToken = ctx.ParseResult.GetValueForOption(mapboxTokenOpt);
            var mapboxTileset = ctx.ParseResult.GetValueForOption(mapboxTilesetOpt);
            var mapboxZoom = ctx.ParseResult.GetValueForOption(mapboxZoomOpt);
            var generateUv = ctx.ParseResult.GetValueForOption(generateUvOpt);
            var uvMetersPerRepeat = ctx.ParseResult.GetValueForOption(uvMetersPerRepeatOpt);
            var alignToTerrain = ctx.ParseResult.GetValueForOption(alignToTerrainOpt);
            var terrainElevationProvider = ctx.ParseResult.GetValueForOption(terrainElevationProviderOpt) ?? "echo";
            var terrainLocalRoot = ctx.ParseResult.GetValueForOption(terrainLocalRootOpt);
            var terrainSrtmBaseUrl = ctx.ParseResult.GetValueForOption(terrainBaseUrlOpt);
            var terrainPersist = ctx.ParseResult.GetValueForOption(terrainPersistOpt);
            var terrainCache = ctx.ParseResult.GetValueForOption(terrainCacheOpt);
            var terrainTreatNoData = ctx.ParseResult.GetValueForOption(terrainTreatNoDataOpt);
            var cacheRoot = ctx.ParseResult.GetValueForOption(vecCacheRootOpt);
            var persistCache = ctx.ParseResult.GetValueForOption(vecPersistCacheOpt);
            var terrainCacheRoot = ctx.ParseResult.GetValueForOption(terrainCacheRootOpt);
            var terrainPersistCache = ctx.ParseResult.GetValueForOption(terrainPersistCacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(vecAirgapOpt);
            var forceFail = ctx.ParseResult.GetValueForOption(vecForceFailOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoVectorCommand.BuildingsToObjAsync(
                bounds,
                output,
                provider,
                mapboxToken,
                mapboxTileset,
                mapboxZoom,
                osmPbf,
                generateUv,
                uvMetersPerRepeat,
                alignToTerrain,
                terrainElevationProvider,
                terrainLocalRoot,
                terrainSrtmBaseUrl,
                terrainPersist,
                terrainCache,
                terrainTreatNoData,
                airgap,
                forceFail,
                cacheRoot,
                persistCache,
                terrainCacheRoot,
                terrainPersistCache,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        // roads-to-obj
        roadsToObjCmd.AddOption(boundsOpt);
        roadsToObjCmd.AddOption(vecOutOpt);
        roadsToObjCmd.AddOption(vectorProviderOpt);
        roadsToObjCmd.AddOption(osmPbfOpt);
        roadsToObjCmd.AddOption(mapboxTokenOpt);
        roadsToObjCmd.AddOption(mapboxTilesetOpt);
        roadsToObjCmd.AddOption(mapboxZoomOpt);
        roadsToObjCmd.AddOption(roadWidthOpt);
        roadsToObjCmd.AddOption(conformToTerrainOpt);
        roadsToObjCmd.AddOption(generateUvOpt);
        roadsToObjCmd.AddOption(uvMetersPerRepeatOpt);
        roadsToObjCmd.AddOption(terrainElevationProviderOpt);
        roadsToObjCmd.AddOption(terrainLocalRootOpt);
        roadsToObjCmd.AddOption(terrainBaseUrlOpt);
        roadsToObjCmd.AddOption(terrainPersistOpt);
        roadsToObjCmd.AddOption(terrainCacheOpt);
        roadsToObjCmd.AddOption(terrainTreatNoDataOpt);
        roadsToObjCmd.AddOption(vecCacheRootOpt);
        roadsToObjCmd.AddOption(vecPersistCacheOpt);
        roadsToObjCmd.AddOption(terrainCacheRootOpt);
        roadsToObjCmd.AddOption(terrainPersistCacheOpt);
        roadsToObjCmd.AddOption(vecAirgapOpt);
        roadsToObjCmd.AddOption(vecForceFailOpt);

        roadsToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(boundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(vecOutOpt) ?? throw new InvalidOperationException("--output is required");
            var provider = ctx.ParseResult.GetValueForOption(vectorProviderOpt) ?? "echo";
            var osmPbf = ctx.ParseResult.GetValueForOption(osmPbfOpt);
            var mapboxToken = ctx.ParseResult.GetValueForOption(mapboxTokenOpt);
            var mapboxTileset = ctx.ParseResult.GetValueForOption(mapboxTilesetOpt);
            var mapboxZoom = ctx.ParseResult.GetValueForOption(mapboxZoomOpt);
            var widthMeters = ctx.ParseResult.GetValueForOption(roadWidthOpt);
            var conformToTerrain = ctx.ParseResult.GetValueForOption(conformToTerrainOpt);
            var generateUv = ctx.ParseResult.GetValueForOption(generateUvOpt);
            var uvMetersPerRepeat = ctx.ParseResult.GetValueForOption(uvMetersPerRepeatOpt);
            var terrainElevationProvider = ctx.ParseResult.GetValueForOption(terrainElevationProviderOpt) ?? "echo";
            var terrainLocalRoot = ctx.ParseResult.GetValueForOption(terrainLocalRootOpt);
            var terrainSrtmBaseUrl = ctx.ParseResult.GetValueForOption(terrainBaseUrlOpt);
            var terrainPersist = ctx.ParseResult.GetValueForOption(terrainPersistOpt);
            var terrainCache = ctx.ParseResult.GetValueForOption(terrainCacheOpt);
            var terrainTreatNoData = ctx.ParseResult.GetValueForOption(terrainTreatNoDataOpt);
            var cacheRoot = ctx.ParseResult.GetValueForOption(vecCacheRootOpt);
            var persistCache = ctx.ParseResult.GetValueForOption(vecPersistCacheOpt);
            var terrainCacheRoot = ctx.ParseResult.GetValueForOption(terrainCacheRootOpt);
            var terrainPersistCache = ctx.ParseResult.GetValueForOption(terrainPersistCacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(vecAirgapOpt);
            var forceFail = ctx.ParseResult.GetValueForOption(vecForceFailOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoVectorCommand.RoadsToObjAsync(
                bounds,
                output,
                provider,
                mapboxToken,
                mapboxTileset,
                mapboxZoom,
                osmPbf,
                widthMeters,
                generateUv,
                uvMetersPerRepeat,
                conformToTerrain,
                terrainElevationProvider,
                terrainLocalRoot,
                terrainSrtmBaseUrl,
                terrainPersist,
                terrainCache,
                terrainTreatNoData,
                airgap,
                forceFail,
                cacheRoot,
                persistCache,
                terrainCacheRoot,
                terrainPersistCache,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        // water-to-obj
        waterToObjCmd.AddOption(boundsOpt);
        waterToObjCmd.AddOption(vecOutOpt);
        waterToObjCmd.AddOption(vectorProviderOpt);
        waterToObjCmd.AddOption(osmPbfOpt);
        waterToObjCmd.AddOption(mapboxTokenOpt);
        waterToObjCmd.AddOption(mapboxTilesetOpt);
        waterToObjCmd.AddOption(mapboxZoomOpt);
        waterToObjCmd.AddOption(conformToTerrainOpt);
        waterToObjCmd.AddOption(waterSurfaceOffsetOpt);
        waterToObjCmd.AddOption(generateUvOpt);
        waterToObjCmd.AddOption(uvMetersPerRepeatOpt);
        waterToObjCmd.AddOption(terrainElevationProviderOpt);
        waterToObjCmd.AddOption(terrainLocalRootOpt);
        waterToObjCmd.AddOption(terrainBaseUrlOpt);
        waterToObjCmd.AddOption(terrainPersistOpt);
        waterToObjCmd.AddOption(terrainCacheOpt);
        waterToObjCmd.AddOption(terrainTreatNoDataOpt);
        waterToObjCmd.AddOption(vecCacheRootOpt);
        waterToObjCmd.AddOption(vecPersistCacheOpt);
        waterToObjCmd.AddOption(terrainCacheRootOpt);
        waterToObjCmd.AddOption(terrainPersistCacheOpt);
        waterToObjCmd.AddOption(vecAirgapOpt);
        waterToObjCmd.AddOption(vecForceFailOpt);

        waterToObjCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(boundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var output = ctx.ParseResult.GetValueForOption(vecOutOpt) ?? throw new InvalidOperationException("--output is required");
            var provider = ctx.ParseResult.GetValueForOption(vectorProviderOpt) ?? "echo";
            var osmPbf = ctx.ParseResult.GetValueForOption(osmPbfOpt);
            var mapboxToken = ctx.ParseResult.GetValueForOption(mapboxTokenOpt);
            var mapboxTileset = ctx.ParseResult.GetValueForOption(mapboxTilesetOpt);
            var mapboxZoom = ctx.ParseResult.GetValueForOption(mapboxZoomOpt);
            var conformToTerrain = ctx.ParseResult.GetValueForOption(conformToTerrainOpt);
            var surfaceOffset = ctx.ParseResult.GetValueForOption(waterSurfaceOffsetOpt);
            var generateUv = ctx.ParseResult.GetValueForOption(generateUvOpt);
            var uvMetersPerRepeat = ctx.ParseResult.GetValueForOption(uvMetersPerRepeatOpt);
            var terrainElevationProvider = ctx.ParseResult.GetValueForOption(terrainElevationProviderOpt) ?? "echo";
            var terrainLocalRoot = ctx.ParseResult.GetValueForOption(terrainLocalRootOpt);
            var terrainSrtmBaseUrl = ctx.ParseResult.GetValueForOption(terrainBaseUrlOpt);
            var terrainPersist = ctx.ParseResult.GetValueForOption(terrainPersistOpt);
            var terrainCache = ctx.ParseResult.GetValueForOption(terrainCacheOpt);
            var terrainTreatNoData = ctx.ParseResult.GetValueForOption(terrainTreatNoDataOpt);
            var cacheRoot = ctx.ParseResult.GetValueForOption(vecCacheRootOpt);
            var persistCache = ctx.ParseResult.GetValueForOption(vecPersistCacheOpt);
            var terrainCacheRoot = ctx.ParseResult.GetValueForOption(terrainCacheRootOpt);
            var terrainPersistCache = ctx.ParseResult.GetValueForOption(terrainPersistCacheOpt);
            var airgap = ctx.ParseResult.GetValueForOption(vecAirgapOpt);
            var forceFail = ctx.ParseResult.GetValueForOption(vecForceFailOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await geoVectorCommand.WaterToObjAsync(
                bounds,
                output,
                provider,
                mapboxToken,
                mapboxTileset,
                mapboxZoom,
                osmPbf,
                generateUv,
                uvMetersPerRepeat,
                conformToTerrain,
                surfaceOffset,
                terrainElevationProvider,
                terrainLocalRoot,
                terrainSrtmBaseUrl,
                terrainPersist,
                terrainCache,
                terrainTreatNoData,
                airgap,
                forceFail,
                cacheRoot,
                persistCache,
                terrainCacheRoot,
                terrainPersistCache,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        geoVectorCmd.AddCommand(buildingsToObjCmd);
        geoVectorCmd.AddCommand(roadsToObjCmd);
        geoVectorCmd.AddCommand(waterToObjCmd);
        root.AddCommand(geoVectorCmd);

        // nexo world
        var worldCmd = new Command("world", "Unity-first world bundle builder (terrain + buildings + roads + water + vegetation)");
        var worldBuildCmd = new Command("build", "Build a world/ bundle into an output directory");
        var worldBoundsOpt = new Option<string>("--bounds", "Bounds: minLat,minLon,maxLat,maxLon") { IsRequired = true };
        var worldOutDirOpt = new Option<DirectoryInfo>("--out-dir", "Output directory for bundle") { IsRequired = true };
        var worldTerrainProviderOpt = new Option<string>("--terrain-elevation-provider", () => "echo", "Terrain elevation provider: echo|local|http|hybrid");
        var worldTerrainLocalRootOpt = new Option<string?>("--terrain-local-root", () => null, "Local SRTM cache directory (for terrain local/hybrid)");
        var worldTerrainBaseUrlOpt = new Option<string?>("--terrain-srtm-base-url", () => null, "Base URL for terrain HTTP downloads (http/hybrid)");
        var worldTerrainPersistOpt = new Option<bool>("--terrain-persist-downloads", () => true, "Persist downloaded terrain tiles into --terrain-local-root (hybrid)");
        var worldTerrainCacheOpt = new Option<bool>("--terrain-cache", () => true, "Enable in-memory cache for terrain elevation provider");
        var worldVectorProviderOpt = new Option<string>("--vector-provider", () => "echo", "Vector provider: echo|osm|mapbox|hybrid");
        var worldOsmPbfOpt = new Option<string?>("--osm-pbf", () => null, "Path to local OSM .pbf extract (required for osm/hybrid)");
        var worldMapboxTokenOpt = new Option<string?>("--mapbox-token", () => null, "Mapbox access token (or set MAPBOX_ACCESS_TOKEN)");
        var worldMapboxTilesetOpt = new Option<string?>("--mapbox-tileset", () => "mapbox.mapbox-streets-v8", "Mapbox tileset id");
        var worldMapboxZoomOpt = new Option<int?>("--mapbox-zoom", () => 15, "Zoom level for tile selection (0-22)");
        var worldTerrainChunkSamplesOpt = new Option<int>("--terrain-chunk-samples", () => 0, "If >0, export terrain as chunked OBJs (grid samples per chunk)");
        var worldTerrainLodFactorsOpt = new Option<string?>("--terrain-lod-factors", () => null, "Optional CSV of LOD factors (e.g. 2,4). Generates additional terrain_lods/*.obj");
        var worldLodTriBudgetsOpt = new Option<string?>("--lod-tri-budgets", () => null, "Optional CSV of triangle budgets per mesh (e.g. 200000,50000). Generates additional *_tri{N} meshes.");
        var worldInstancesChunkSamplesOpt = new Option<int>("--instances-chunk-samples", () => 0, "If >0, export instances as chunked JSON files (grid samples per chunk)");
        var worldTerrainImageryOpt = new Option<bool>("--terrain-imagery", () => false, "If set, download and stitch a Mapbox raster mosaic and export terrain_texture.png + terrain.mtl with UVs");
        var worldTerrainImageryTilesetOpt = new Option<string?>("--terrain-imagery-tileset", () => "mapbox.satellite", "Mapbox raster tileset id for terrain imagery");
        var worldTerrainImageryFormatOpt = new Option<string?>("--terrain-imagery-format", () => "jpg90", "Mapbox raster format (e.g. jpg90, png)");
        var worldTerrainImageryZoomOpt = new Option<int?>("--terrain-imagery-zoom", () => null, "Optional zoom override for terrain imagery (defaults to --mapbox-zoom)");
        var worldWaterFlattenOpt = new Option<bool>("--water-flatten-to-terrain", () => false, "If set, flatten each water polygon to a single sampled terrain height (instead of per-vertex)");
        var worldMeshFormatOpt = new Option<string>("--mesh-format", () => "obj", "Mesh format for exported geometry: obj|gltf|gltf-scene|glb");
        var worldProjectionOpt = new Option<string>("--projection", () => "auto", "Projection for mapping lat/lon to local meters: auto|local|utm|webmercator");
        var worldVectorTexturesOpt = new Option<bool>("--vector-textures", () => false, "If set, generate simple procedural textures for buildings/roads/water and reference them in materials.json (and world.mtl for OBJ).");
        var worldAirgapOpt = new Option<bool>("--airgap", () => false, "Air-gapped mode: forces deterministic only");

        worldBuildCmd.AddOption(worldBoundsOpt);
        worldBuildCmd.AddOption(worldOutDirOpt);
        worldBuildCmd.AddOption(worldTerrainProviderOpt);
        worldBuildCmd.AddOption(worldTerrainLocalRootOpt);
        worldBuildCmd.AddOption(worldTerrainBaseUrlOpt);
        worldBuildCmd.AddOption(worldTerrainPersistOpt);
        worldBuildCmd.AddOption(worldTerrainCacheOpt);
        worldBuildCmd.AddOption(worldVectorProviderOpt);
        worldBuildCmd.AddOption(worldOsmPbfOpt);
        worldBuildCmd.AddOption(worldMapboxTokenOpt);
        worldBuildCmd.AddOption(worldMapboxTilesetOpt);
        worldBuildCmd.AddOption(worldMapboxZoomOpt);
        worldBuildCmd.AddOption(worldTerrainChunkSamplesOpt);
        worldBuildCmd.AddOption(worldTerrainLodFactorsOpt);
        worldBuildCmd.AddOption(worldLodTriBudgetsOpt);
        worldBuildCmd.AddOption(worldInstancesChunkSamplesOpt);
        worldBuildCmd.AddOption(worldTerrainImageryOpt);
        worldBuildCmd.AddOption(worldTerrainImageryTilesetOpt);
        worldBuildCmd.AddOption(worldTerrainImageryFormatOpt);
        worldBuildCmd.AddOption(worldTerrainImageryZoomOpt);
        worldBuildCmd.AddOption(worldWaterFlattenOpt);
        worldBuildCmd.AddOption(worldMeshFormatOpt);
        worldBuildCmd.AddOption(worldProjectionOpt);
        worldBuildCmd.AddOption(worldVectorTexturesOpt);
        worldBuildCmd.AddOption(worldAirgapOpt);

        worldBuildCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var bounds = ctx.ParseResult.GetValueForOption(worldBoundsOpt) ?? throw new InvalidOperationException("--bounds is required");
            var outDir = ctx.ParseResult.GetValueForOption(worldOutDirOpt) ?? throw new InvalidOperationException("--out-dir is required");
            var terrainProvider = ctx.ParseResult.GetValueForOption(worldTerrainProviderOpt) ?? "echo";
            var terrainLocalRoot = ctx.ParseResult.GetValueForOption(worldTerrainLocalRootOpt);
            var terrainBaseUrl = ctx.ParseResult.GetValueForOption(worldTerrainBaseUrlOpt);
            var terrainPersist = ctx.ParseResult.GetValueForOption(worldTerrainPersistOpt);
            var terrainCache = ctx.ParseResult.GetValueForOption(worldTerrainCacheOpt);
            var vectorProvider = ctx.ParseResult.GetValueForOption(worldVectorProviderOpt) ?? "echo";
            var osmPbf = ctx.ParseResult.GetValueForOption(worldOsmPbfOpt);
            var mapboxToken = ctx.ParseResult.GetValueForOption(worldMapboxTokenOpt);
            var mapboxTileset = ctx.ParseResult.GetValueForOption(worldMapboxTilesetOpt);
            var mapboxZoom = ctx.ParseResult.GetValueForOption(worldMapboxZoomOpt);
            var terrainChunkSamples = ctx.ParseResult.GetValueForOption(worldTerrainChunkSamplesOpt);
            var terrainLodFactors = ctx.ParseResult.GetValueForOption(worldTerrainLodFactorsOpt);
            var lodTriBudgets = ctx.ParseResult.GetValueForOption(worldLodTriBudgetsOpt);
            var instancesChunkSamples = ctx.ParseResult.GetValueForOption(worldInstancesChunkSamplesOpt);
            var terrainImagery = ctx.ParseResult.GetValueForOption(worldTerrainImageryOpt);
            var terrainImageryTileset = ctx.ParseResult.GetValueForOption(worldTerrainImageryTilesetOpt);
            var terrainImageryFormat = ctx.ParseResult.GetValueForOption(worldTerrainImageryFormatOpt);
            var terrainImageryZoom = ctx.ParseResult.GetValueForOption(worldTerrainImageryZoomOpt);
            var waterFlatten = ctx.ParseResult.GetValueForOption(worldWaterFlattenOpt);
            var meshFormat = ctx.ParseResult.GetValueForOption(worldMeshFormatOpt) ?? "obj";
            var projection = ctx.ParseResult.GetValueForOption(worldProjectionOpt) ?? "auto";
            var vectorTextures = ctx.ParseResult.GetValueForOption(worldVectorTexturesOpt);
            var airgap = ctx.ParseResult.GetValueForOption(worldAirgapOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await worldCommand.BuildAsync(
                bounds,
                outDir,
                terrainProvider,
                terrainLocalRoot,
                terrainBaseUrl,
                terrainPersist,
                terrainCache,
                vectorProvider,
                osmPbf,
                mapboxToken,
                mapboxTileset,
                mapboxZoom,
                terrainChunkSamples,
                terrainLodFactors,
                lodTriBudgets,
                instancesChunkSamples,
                terrainImagery,
                terrainImageryTileset,
                terrainImageryFormat,
                terrainImageryZoom,
                waterFlatten,
                meshFormat,
                projection,
                vectorTextures,
                airgap,
                json,
                verbose,
                CancellationToken.None);
            ctx.ExitCode = exitCode;
        });

        var worldValidateCmd = new Command("validate", "Validate a world bundle directory against its manifest");
        var worldBundleDirOpt = new Option<DirectoryInfo>("--bundle-dir", "World bundle directory (contains manifest.json)") { IsRequired = true };
        worldValidateCmd.AddOption(worldBundleDirOpt);
        worldValidateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var dir = ctx.ParseResult.GetValueForOption(worldBundleDirOpt) ?? throw new InvalidOperationException("--bundle-dir is required");
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            ctx.ExitCode = await worldCommand.ValidateAsync(dir, json, verbose, CancellationToken.None);
        });

        worldCmd.AddCommand(worldBuildCmd);
        worldCmd.AddCommand(worldValidateCmd);
        root.AddCommand(worldCmd);

        // nexo build
        var buildCmd = new BuildCommand();
        root.AddCommand(buildCmd);

        // nexo ci
        var ciCmd = new CiCommand();
        root.AddCommand(ciCmd);

        // nexo aggregate
        var aggregateCmd = new AggregateCommand();
        root.AddCommand(aggregateCmd);

        // nexo docker
        var dockerCmd = new DockerCommand();
        root.AddCommand(dockerCmd);

        // nexo diff
        var diffCmd = new DiffCommand();
        root.AddCommand(diffCmd);

        // nexo review (replaces review-summary-md.sh)
        root.AddCommand(new ReviewCommand());

        // nexo report
        var reportCmd = new ReportCommand();
        root.AddCommand(reportCmd);

        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);
        root.AddCommand(configCmd);
        root.AddCommand(backgroundAgentCmd);
        root.AddCommand(testCmd);
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
        services.AddHttpClient();
        services.AddHttpClient("geoterrain.srtm");
        services.AddHttpClient("geovector.mapbox");

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

        // Register loop kernel for hot paths (toggleable via env vars).
        services.AddSingleton<ILoopKernel>(sp =>
        {
            ILoopKernel k = new SequentialLoopKernel();

            var enableParallel = string.Equals(Environment.GetEnvironmentVariable("NEXO_LOOP_PARALLEL"), "1", StringComparison.OrdinalIgnoreCase);
            if (enableParallel)
            {
                k = new ParallelLoopKernel(k);
            }

            var instrument = string.Equals(Environment.GetEnvironmentVariable("NEXO_LOOP_INSTRUMENT"), "1", StringComparison.OrdinalIgnoreCase);
            if (instrument)
            {
                k = new InstrumentedLoopKernel(k, sp.GetRequiredService<ILogger<InstrumentedLoopKernel>>());
            }

            return k;
        });

        // Register orchestration layer
        services.AddNexoOrchestration();

        // Persistence (in-memory by default; replace with adapter for SQLite/Postgres/etc. to avoid DB lock-in)
        services.AddNexoPersistence();

        // Background agents (CLI-only: no hosted service)
        services.AddBackgroundAgents(registerHostedService: false);
        services.AddBackgroundAgentsRAG();
        services.TryAddSingleton<Nexo.BackgroundAgents.WebSearch.IWebSearchProvider, Nexo.BackgroundAgents.WebSearch.MockWebSearchProvider>();
        // Dog-food: optimizer agents run the app's own analysis pipeline
        services.TryAddSingleton<Nexo.BackgroundAgents.Optimization.ICodeAnalysisRunner, Nexo.CLI.Commands.BackgroundAgent.CodeAnalysisRunnerAdapter>();
        // Dog-food: tester agents run the app's own test pipeline
        services.TryAddSingleton<Nexo.BackgroundAgents.Testing.ITestRunRunner, Nexo.CLI.Commands.BackgroundAgent.TestRunRunnerAdapter>();
        // Dog-food: extender agents run self-extend cycle (LLM + tools with path policy)
        services.TryAddSingleton<Nexo.BackgroundAgents.Extending.ISelfExtendRunner, Nexo.CLI.Commands.BackgroundAgent.SelfExtendRunnerAdapter>();

        // Register base IModel as hot-swappable (provider-backed with deterministic fallback),
        // then wrap it with OrchestrationRuntimeModelDecorator so `nexo orchestrate --runtime-spec`
        // can affect Architect/Negotiation without changing callsites.
        services.AddSingleton<Nexo.Infrastructure.Execution.Models.HotSwappableModel>(sp =>
        {
            var providerFactory = sp.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>();
            var providerBacked = new Nexo.Infrastructure.Execution.Models.ProviderBackedModel(
                providerFactory,
                sp.GetRequiredService<ILogger<Nexo.Infrastructure.Execution.Models.ProviderBackedModel>>());

            return new Nexo.Infrastructure.Execution.Models.HotSwappableModel(
                providerBacked,
                sp.GetRequiredService<ILogger<Nexo.Infrastructure.Execution.Models.HotSwappableModel>>());
        });

        services.AddSingleton<Nexo.Abstractions.IModel>(sp =>
        {
            var accessor = sp.GetRequiredService<IOrchestrationRuntimeSpecAccessor>();
            var inner = sp.GetRequiredService<Nexo.Infrastructure.Execution.Models.HotSwappableModel>();
            return new OrchestrationRuntimeModelDecorator(
                inner,
                accessor,
                sp.GetRequiredService<ILogger<OrchestrationRuntimeModelDecorator>>());
        });
        
        // Register IProviderFactory for agent execution
        services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory, Nexo.Infrastructure.Execution.ProviderFactory>();

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
        services.AddScoped<UnityCommand>();
        services.AddScoped<DemoCommand>();
        // Register geospatial commands with interfaces for testability
        services.AddScoped<Nexo.CLI.Commands.GeoTerrain.IGeoTerrainCommand, Nexo.CLI.Commands.GeoTerrain.GeoTerrainCommand>();
        services.AddScoped<Nexo.CLI.Commands.GeoVector.IGeoVectorCommand, Nexo.CLI.Commands.GeoVector.GeoVectorCommand>();
        services.AddScoped<Nexo.CLI.Commands.World.IWorldCommand, Nexo.CLI.Commands.World.WorldCommand>();
        // Also register concrete types for CLI command handlers
        services.AddScoped<Nexo.CLI.Commands.GeoTerrain.GeoTerrainCommand>();
        services.AddScoped<Nexo.CLI.Commands.GeoVector.GeoVectorCommand>();
        services.AddScoped<Nexo.CLI.Commands.World.WorldCommand>();
        services.AddScoped<BackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.ExecuteBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.LogsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.MetricsBackgroundAgentCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
        services.AddScoped<Nexo.CLI.Commands.BackgroundAgent.WebSearchCommand>();

        // Register test runner
        services.AddScoped<Nexo.Core.Application.Testing.Ports.ITestRunner, Nexo.Infrastructure.Testing.TestRunnerAdapter>();
        
        // Register execution platform for portable multi-platform testing
        // Default to Docker, but users can override with Rancher, Kubernetes, etc.
        services.AddSingleton<Nexo.Infrastructure.Testing.ExecutionPlatform.IExecutionPlatform>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform>>();
            return new Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform(logger);
        });
        
        // Also register Docker service for backward compatibility (if needed)
        services.AddSingleton<Nexo.Infrastructure.Testing.Docker.IDockerService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Nexo.Infrastructure.Testing.Docker.DockerService>>();
            return new Nexo.Infrastructure.Testing.Docker.DockerService(logger);
        });

        // Register code analysis service for portable compilation/decompilation
        services.AddSingleton<Nexo.Infrastructure.Testing.CodeAnalysis.ICodeAnalysisService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService>>();
            return new Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService(logger);
        });

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

        // Register filesystem abstraction (keeps System.IO out of Core.Application)
        services.AddSingleton<Nexo.Core.Application.Common.Ports.ITextFileSystem, Nexo.Infrastructure.IO.LocalTextFileSystem>();

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
}
