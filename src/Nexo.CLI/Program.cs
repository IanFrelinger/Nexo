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
static class Program
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

        // nexo analyze (resolves from host lazily - host built only when heavy command runs)
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
                var cmd = ServiceProvider.GetRequiredService<AnalyzeCommand>();
                var exitCode = await cmd.ExecuteAsync(path, json, verbose);
                Environment.Exit(exitCode);
            },
            analyzeCmd.Options[0] as Option<DirectoryInfo> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        analyzeCmd.AddCommand(new AnalyzeBricksCommand());

        // nexo validate
        var validateCmd = new Command("validate", "Run architecture tests/contract checks quickly")
        {
            new Option<string?>("--filter", "Optional test filter (Category/Trait)")
        };
        validateCmd.SetHandler(
            async (string? filter, bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<ValidateCommand>();
                var exitCode = await cmd.ExecuteAsync(filter, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<AgentCommand>();
                var exitCode = await cmd.ExecuteAsync(name, input, json, verbose);
                Environment.Exit(exitCode);
            },
            agentCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            agentCmd.Options[1] as Option<FileInfo?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);

        // nexo agent list
        var listAgentsCmd = new Command("list", "List available agents");
        listAgentsCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<ListAgentsCommand>();
                var exitCode = await cmd.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        agentCmd.AddCommand(listAgentsCmd);

        // nexo config
        var configCmd = new Command("config", "View or manage configuration");
        var configShowCmd = new Command("show", "Show current configuration");
        configShowCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configShowCmd);
        var configValidateCmd = new Command("validate", "Validate configuration");
        configValidateCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.ValidateAsync(json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.ExportAsync(path.FullName, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.ImportAsync(path.FullName, json, verbose);
                Environment.Exit(exitCode);
            },
            configImportPathOpt,
            jsonOpt,
            verboseOpt);
        configCmd.AddCommand(configImportCmd);
        var configSetModeStepArg = new Argument<string>("step-id", "Step ID");
        var configSetModeModeArg = new Argument<string>("mode", "Execution mode: deterministic | agentic");
        var configSetModeCmd = new Command("set-mode", "Set execution mode for a step (hot-swap)") { configSetModeStepArg, configSetModeModeArg };
        configSetModeCmd.SetHandler(
            async (string stepId, string modeStr) =>
            {
                var mode = modeStr.Trim().ToLowerInvariant() switch
                {
                    "deterministic" => Nexo.Core.Application.Execution.Models.ExecutionMode.Deterministic,
                    "agentic" => Nexo.Core.Application.Execution.Models.ExecutionMode.Agentic,
                    _ => throw new ArgumentException($"Mode must be 'deterministic' or 'agentic', got: {modeStr}")
                };
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.SetModeAsync(stepId, mode);
                Environment.Exit(exitCode);
            },
            configSetModeStepArg,
            configSetModeModeArg);
        configCmd.AddCommand(configSetModeCmd);
        configCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<ConfigCommand>();
                var exitCode = await cmd.ExecuteAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);

        // nexo background-agent
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
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                var exitCode = await cmd.ListAsync(formatJson, status, role, sensitivity);
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
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                var exitCode = await cmd.ShowAsync(id, formatJson);
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
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                var exitCode = await cmd.StartAsync(id, formatJson);
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
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                var exitCode = await cmd.StopAsync(id, formatJson);
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
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                var exitCode = await cmd.RestartAsync(id, formatJson);
                Environment.Exit(exitCode);
            },
            restartBgIdOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(restartBgCmd);

        // nexo background-agent autoscale
        var autoscaleCmd = new Command("autoscale", "Demand-based autoscaling for role agents");
        var autoscaleApplyRoleOpt = new Option<string>("--role", "Role to scale (e.g., extender, tester)") { IsRequired = true };
        var autoscaleApplyDemandOpt = new Option<int>("--demand", "Current demand score for this role") { IsRequired = true };
        var autoscaleApplyMinOpt = new Option<int>("--min-agents", () => 0, "Minimum desired agents for the role");
        var autoscaleApplyMaxOpt = new Option<int>("--max-agents", () => 5, "Maximum desired agents for the role");
        var autoscaleApplyUnitsOpt = new Option<int>("--units-per-agent", () => 1, "Demand units handled per agent (higher value = fewer agents)");
        var autoscaleApplyIdleOpt = new Option<int>("--idle-seconds", () => 0, "Idle threshold before stopping surplus autoscaled agents");
        var autoscaleApplyCmd = new Command("apply", "Apply one autoscale decision")
        {
            autoscaleApplyRoleOpt,
            autoscaleApplyDemandOpt,
            autoscaleApplyMinOpt,
            autoscaleApplyMaxOpt,
            autoscaleApplyUnitsOpt,
            autoscaleApplyIdleOpt
        };
        autoscaleApplyCmd.SetHandler(
            async (string role, int demand, int minAgents, int maxAgents, int unitsPerAgent, int idleSeconds, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<BackgroundAgentCommand>();
                Environment.Exit(await cmd.AutoScaleAsync(role, demand, minAgents, maxAgents, unitsPerAgent, idleSeconds, formatJson));
            },
            autoscaleApplyRoleOpt,
            autoscaleApplyDemandOpt,
            autoscaleApplyMinOpt,
            autoscaleApplyMaxOpt,
            autoscaleApplyUnitsOpt,
            autoscaleApplyIdleOpt,
            jsonOpt);
        autoscaleCmd.AddCommand(autoscaleApplyCmd);
        backgroundAgentCmd.AddCommand(autoscaleCmd);

        // nexo background-agent execute
        var executeBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var executeBgAsyncOpt = new Option<bool>("--async", "Run execution asynchronously (don't wait)");
        var executeBgCmd = new Command("execute", "Manually run one execution of a background agent") { executeBgIdOpt, executeBgAsyncOpt };
        executeBgCmd.SetHandler(
            async (string id, bool runAsync, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ExecuteBackgroundAgentCommand>();
                Environment.Exit(await cmd.ExecuteAsync(id, runAsync, formatJson));
            },
            executeBgIdOpt, executeBgAsyncOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(executeBgCmd);

        // nexo background-agent logs
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
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.LogsBackgroundAgentCommand>();
                TimeSpan? sinceTs = ParseSince(since);
                Environment.Exit(await cmd.ExecuteAsync(id, tail, level, sinceTs, formatJson));
            },
            logsBgIdOpt,
            logsBgCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            logsBgCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            logsBgCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt);
        backgroundAgentCmd.AddCommand(logsBgCmd);

        // nexo background-agent metrics
        var metricsBgIdOpt = new Option<string>("--id", "Agent ID") { IsRequired = true };
        var metricsBgCmd = new Command("metrics", "Show agent performance metrics") { metricsBgIdOpt };
        metricsBgCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.MetricsBackgroundAgentCommand>();
                Environment.Exit(await cmd.ExecuteAsync(id, formatJson));
            },
            metricsBgIdOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(metricsBgCmd);

        // nexo background-agent stats
        var statsAgentOpt = new Option<string?>("--agent", () => null, "Filter to a specific agent id");
        var statsRoleOpt = new Option<string?>("--role", () => null, "Filter to a specific role (planner, optimizer, tester, extender, ...)");
        var statsSinceOpt = new Option<double?>("--since-hours", () => null, "Only events newer than now-N hours");
        var statsBgCmd = new Command("stats", "Aggregate the cycle event log (cycles.jsonl) into per-agent throughput / denial / error stats")
        {
            statsAgentOpt,
            statsRoleOpt,
            statsSinceOpt
        };
        statsBgCmd.SetHandler(
            async (string? agent, string? role, double? since, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.StatsBackgroundAgentCommand>();
                Environment.Exit(await cmd.ExecuteAsync(agent, role, since, formatJson));
            },
            statsAgentOpt, statsRoleOpt, statsSinceOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(statsBgCmd);

        // nexo background-agent observations — read the structured observations log.
        // Companion to `stats`: where stats summarises agent execution, observations
        // surfaces the *facts* agents collectively published (build/test outcomes,
        // analysis violations, agent actions). Useful for quickly answering
        // "what does the daemon currently know about the codebase?".
        var obsSourceOpt = new Option<string?>("--source", () => null, "Filter to a specific source (typically the agent id)");
        var obsKindOpt = new Option<string?>("--kind", () => null, "Filter by kind (Build, Test, Analysis, AgentAction, UserSignal)");
        var obsSinceOpt = new Option<double?>("--since-hours", () => null, "Only observations newer than now-N hours");
        var obsTailOpt = new Option<int?>("--tail", () => null, "Show only the most recent N rows after filtering");
        var obsSummaryOpt = new Option<bool>("--summary", () => false, "Group counts by source/kind/severity instead of listing rows");
        var observationsBgCmd = new Command("observations", "Inspect the structured observations log (observations.jsonl)")
        {
            obsSourceOpt, obsKindOpt, obsSinceOpt, obsTailOpt, obsSummaryOpt
        };
        observationsBgCmd.SetHandler(
            async (string? source, string? kind, double? since, int? tail, bool summary, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObservationsBackgroundAgentCommand>();
                Environment.Exit(await cmd.ExecuteAsync(source, kind, since, tail, summary, formatJson));
            },
            obsSourceOpt, obsKindOpt, obsSinceOpt, obsTailOpt, obsSummaryOpt, jsonOpt);
        backgroundAgentCmd.AddCommand(observationsBgCmd);

        // nexo background-agent objectives — operator front door for the structured backlog.
        // Mirrors the same store the planner reads from, so adds here are picked up by the
        // next planner cycle automatically. Subcommands: list / show / add / complete /
        // block / unblock / stats. Kept under background-agent (not a top-level group)
        // because the backlog is meaningful only in the context of a daemon that consumes it.
        var objectivesBgCmd = new Command("objectives", "Manage the planner's objective backlog");

        var objListStatusOpt = new Option<string?>("--status", () => null, "Filter by status (Pending, InProgress, Done, Blocked)");
        var objListTagOpt = new Option<string?>("--tag", () => null, "Filter by tag (case-insensitive exact match)");
        var objListCmd = new Command("list", "List backlog items, sorted by status then priority")
        {
            objListStatusOpt, objListTagOpt
        };
        objListCmd.SetHandler(async (string? status, string? tag, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.ListAsync(status, tag, formatJson));
        }, objListStatusOpt, objListTagOpt, jsonOpt);
        objectivesBgCmd.AddCommand(objListCmd);

        var objShowIdArg = new Argument<string>("id", "Objective id");
        var objShowCmd = new Command("show", "Show one objective's full body and metadata") { objShowIdArg };
        objShowCmd.SetHandler(async (string id, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.ShowAsync(id, formatJson));
        }, objShowIdArg, jsonOpt);
        objectivesBgCmd.AddCommand(objShowCmd);

        var objAddIdOpt = new Option<string>("--id", "Stable slug used as the filename") { IsRequired = true };
        var objAddTitleOpt = new Option<string>("--title", "One-line human-readable title") { IsRequired = true };
        var objAddPriorityOpt = new Option<int>("--priority", () => 100, "Lower number = higher priority (1 highest)");
        var objAddTagsOpt = new Option<string[]>("--tag", () => Array.Empty<string>(), "Tag (repeatable)") { AllowMultipleArgumentsPerToken = true };
        var objAddBodyOpt = new Option<string?>("--body", () => null, "Inline markdown body (mutually exclusive with --body-file)");
        var objAddBodyFileOpt = new Option<string?>("--body-file", () => null, "Read markdown body from this file");
        var objAddCmd = new Command("add", "Add a new pending objective")
        {
            objAddIdOpt, objAddTitleOpt, objAddPriorityOpt, objAddTagsOpt, objAddBodyOpt, objAddBodyFileOpt
        };
        objAddCmd.SetHandler(async (string id, string title, int priority, string[] tags, string? body, string? bodyFile, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.AddAsync(id, title, priority, tags, body, bodyFile, formatJson));
        }, objAddIdOpt, objAddTitleOpt, objAddPriorityOpt, objAddTagsOpt, objAddBodyOpt, objAddBodyFileOpt, jsonOpt);
        objectivesBgCmd.AddCommand(objAddCmd);

        var objCompleteIdArg = new Argument<string>("id", "Objective id (must currently be InProgress)");
        var objCompleteNoteOpt = new Option<string?>("--note", () => null, "Optional note appended to the body");
        var objCompleteCmd = new Command("complete", "Mark an in-progress objective as Done")
        {
            objCompleteIdArg, objCompleteNoteOpt
        };
        objCompleteCmd.SetHandler(async (string id, string? note, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.CompleteAsync(id, note, formatJson));
        }, objCompleteIdArg, objCompleteNoteOpt, jsonOpt);
        objectivesBgCmd.AddCommand(objCompleteCmd);

        var objBlockIdArg = new Argument<string>("id", "Objective id");
        var objBlockReasonOpt = new Option<string>("--reason", "Why the objective is blocked") { IsRequired = true };
        var objBlockCmd = new Command("block", "Mark an objective as Blocked")
        {
            objBlockIdArg, objBlockReasonOpt
        };
        objBlockCmd.SetHandler(async (string id, string reason, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.BlockAsync(id, reason, formatJson));
        }, objBlockIdArg, objBlockReasonOpt, jsonOpt);
        objectivesBgCmd.AddCommand(objBlockCmd);

        var objUnblockIdArg = new Argument<string>("id", "Objective id");
        var objUnblockCmd = new Command("unblock", "Move a blocked objective back to Pending") { objUnblockIdArg };
        objUnblockCmd.SetHandler(async (string id, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.UnblockAsync(id, formatJson));
        }, objUnblockIdArg, jsonOpt);
        objectivesBgCmd.AddCommand(objUnblockCmd);

        var objReportIdOpt = new Option<string?>("--id", () => null, "Limit report to one objective");
        var objReportStatusOpt = new Option<string?>("--status", () => null, "Filter by status (Pending, InProgress, Done, Blocked)");
        var objReportSinceOpt = new Option<double?>("--since-hours", () => null, "Only count observations newer than now-N hours");
        var objReportCmd = new Command("report", "Cross-correlate objectives with observations (per-objective build/test/error counts)")
        {
            objReportIdOpt, objReportStatusOpt, objReportSinceOpt
        };
        objReportCmd.SetHandler(async (string? id, string? status, double? since, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.ReportAsync(id, status, since, formatJson));
        }, objReportIdOpt, objReportStatusOpt, objReportSinceOpt, jsonOpt);
        objectivesBgCmd.AddCommand(objReportCmd);

        var objStatsCmd = new Command("stats", "Per-status counts and per-tag breakdown of the backlog");
        objStatsCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ObjectivesBackgroundAgentCommand>();
            Environment.Exit(await cmd.StatsAsync(formatJson));
        }, jsonOpt);
        objectivesBgCmd.AddCommand(objStatsCmd);

        backgroundAgentCmd.AddCommand(objectivesBgCmd);

        // nexo background-agent proposals — operator front door for the forge change
        // proposal queue. Mirrors the objectives CLI shape so operators only learn
        // one mental model. Subcommands: list / show / approve / reject / apply / stats.
        var proposalsBgCmd = new Command("proposals", "Manage forge-mediated change proposals");

        var propListStatusOpt = new Option<string?>("--status", () => null, "Filter by status (Proposed, Approved, Rejected, Applied, Stale)");
        var propListTargetOpt = new Option<string?>("--target-prefix", () => null, "Filter by target path prefix");
        var propListCmd = new Command("list", "List proposals, sorted by status then most-recent update")
        {
            propListStatusOpt, propListTargetOpt
        };
        propListCmd.SetHandler(async (string? status, string? targetPrefix, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.ListAsync(status, targetPrefix, formatJson));
        }, propListStatusOpt, propListTargetOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propListCmd);

        var propShowIdArg = new Argument<string>("id", "Proposal id");
        var propShowDiffOpt = new Option<bool>("--show-diff", () => false, "Include the proposed file content in the output");
        var propShowCmd = new Command("show", "Show one proposal's metadata (and optionally its proposed content)")
        {
            propShowIdArg, propShowDiffOpt
        };
        propShowCmd.SetHandler(async (string id, bool showDiff, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.ShowAsync(id, showDiff, formatJson));
        }, propShowIdArg, propShowDiffOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propShowCmd);

        var propApproveIdArg = new Argument<string>("id", "Proposal id (must currently be Proposed)");
        var propApproveByOpt = new Option<string?>("--approver", () => null, "Operator approving the change");
        var propApproveNoteOpt = new Option<string?>("--note", () => null, "Optional approval note");
        var propApproveCmd = new Command("approve", "Approve a Proposed change so it can be applied")
        {
            propApproveIdArg, propApproveByOpt, propApproveNoteOpt
        };
        propApproveCmd.SetHandler(async (string id, string? approver, string? note, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.ApproveAsync(id, approver, note, formatJson));
        }, propApproveIdArg, propApproveByOpt, propApproveNoteOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propApproveCmd);

        var propRejectIdArg = new Argument<string>("id", "Proposal id (must currently be Proposed)");
        var propRejectByOpt = new Option<string?>("--reviewer", () => null, "Operator rejecting the change");
        var propRejectNoteOpt = new Option<string?>("--note", () => null, "Why the change was rejected");
        var propRejectCmd = new Command("reject", "Reject a Proposed change")
        {
            propRejectIdArg, propRejectByOpt, propRejectNoteOpt
        };
        propRejectCmd.SetHandler(async (string id, string? reviewer, string? note, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.RejectAsync(id, reviewer, note, formatJson));
        }, propRejectIdArg, propRejectByOpt, propRejectNoteOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propRejectCmd);

        var propApplyIdArg = new Argument<string>("id", "Approved proposal id");
        var propApplyRootOpt = new Option<string>("--repo-root", () => Directory.GetCurrentDirectory(), "Repo root the target_path is resolved against");
        var propApplyForceOpt = new Option<bool>("--force", () => false, "Apply even if the file's sha256 has drifted from the proposal's BaseSha256");
        var propApplyCmd = new Command("apply", "Write an Approved proposal to disk and mark it Applied")
        {
            propApplyIdArg, propApplyRootOpt, propApplyForceOpt
        };
        propApplyCmd.SetHandler(async (string id, string root, bool force, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.ApplyAsync(id, root, force, formatJson));
        }, propApplyIdArg, propApplyRootOpt, propApplyForceOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propApplyCmd);

        var propJanProposedOpt = new Option<double?>("--proposed-ttl-hours", () => null, "Override Proposed TTL (default 72h)");
        var propJanApprovedOpt = new Option<double?>("--approved-ttl-hours", () => null, "Override Approved TTL (default 24h)");
        var propJanitorCmd = new Command("janitor", "Run the janitor sweep once: stale anything past its TTL")
        {
            propJanProposedOpt, propJanApprovedOpt
        };
        propJanitorCmd.SetHandler(async (double? proposedTtl, double? approvedTtl, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.JanitorAsync(proposedTtl, approvedTtl, formatJson));
        }, propJanProposedOpt, propJanApprovedOpt, jsonOpt);
        proposalsBgCmd.AddCommand(propJanitorCmd);

        var propStatsCmd = new Command("stats", "Per-status counts of the proposal queue");
        propStatsCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ProposalsBackgroundAgentCommand>();
            Environment.Exit(await cmd.StatsAsync(formatJson));
        }, jsonOpt);
        proposalsBgCmd.AddCommand(propStatsCmd);

        backgroundAgentCmd.AddCommand(proposalsBgCmd);

        // nexo background-agent mode
        var modeCmd = new Command("mode", "Get or set aggressiveness mode (passive, semi-active, active, ambient)");
        var modeGetCmd = new Command("get", "Get current mode");
        modeGetCmd.SetHandler(
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ModeBackgroundAgentCommand>();
                Environment.Exit(await cmd.GetAsync(formatJson));
            },
            jsonOpt);
        modeCmd.AddCommand(modeGetCmd);
        var modeSetValueOpt = new Option<string>("--value", "Mode: passive, semi-active, active, ambient") { IsRequired = true };
        var modeSetCmd = new Command("set", "Set mode (switchable at runtime without restart)") { modeSetValueOpt };
        modeSetCmd.SetHandler(
            async (string value, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.ModeBackgroundAgentCommand>();
                Environment.Exit(await cmd.SetAsync(value, formatJson));
            },
            modeSetValueOpt, jsonOpt);
        modeCmd.AddCommand(modeSetCmd);
        backgroundAgentCmd.AddCommand(modeCmd);

        // nexo background-agent sensitivity
        var sensitivityCmd = new Command("sensitivity", "Manage data sensitivity levels");
        var sensListCmd = new Command("list", "List sensitivity levels");
        sensListCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
            Environment.Exit(await cmd.ListAsync(formatJson));
        }, jsonOpt);
        sensitivityCmd.AddCommand(sensListCmd);
        var sensShowNameOpt = new Option<string>("--name", "Sensitivity level name") { IsRequired = true };
        var sensShowCmd = new Command("show", "Show a sensitivity level") { sensShowNameOpt };
        sensShowCmd.SetHandler(async (string name, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
            Environment.Exit(await cmd.ShowAsync(name, formatJson));
        }, sensShowNameOpt, jsonOpt);
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
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
                Environment.Exit(await cmd.AddAsync(name, value, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? "", formatJson));
            },
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
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
                Environment.Exit(await cmd.UpdateAsync(name, value, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? "", formatJson));
            },
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
        sensRemoveCmd.SetHandler(async (string name, bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.SensitivityCommand>();
            Environment.Exit(await cmd.RemoveAsync(name, formatJson));
        }, sensRemoveNameOpt, jsonOpt);
        sensitivityCmd.AddCommand(sensRemoveCmd);
        backgroundAgentCmd.AddCommand(sensitivityCmd);

        // nexo background-agent rag
        var ragCmd = new Command("rag", "RAG (Retrieval Augmented Generation) operations");
        var ragIndexPathsOpt = new Option<string[]>("--paths", "Paths to index (files or directories)") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        var ragIndexSensOpt = new Option<string?>("--sensitivity", "Default sensitivity level for indexed documents");
        var ragIndexCmd = new Command("index", "Index paths into RAG store") { ragIndexPathsOpt, ragIndexSensOpt };
        ragIndexCmd.SetHandler(
            async (string[] paths, string? sensitivity, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
                Environment.Exit(await cmd.IndexAsync(paths ?? Array.Empty<string>(), sensitivity, formatJson));
            },
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
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
                Environment.Exit(await cmd.SearchAsync(query, maxResults, minScore, maxSensitivity, formatJson));
            },
            ragSearchQueryOpt,
            ragSearchCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            ragSearchCmd.Options[2] as Option<double> ?? throw new InvalidOperationException(),
            ragSearchCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt);
        ragCmd.AddCommand(ragSearchCmd);
        var ragStatsCmd = new Command("stats", "Show RAG store statistics");
        ragStatsCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
            Environment.Exit(await cmd.StatsAsync(formatJson));
        }, jsonOpt);
        ragCmd.AddCommand(ragStatsCmd);
        var ragClearCmd = new Command("clear", "Clear RAG store");
        ragClearCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.RAGCommand>();
            Environment.Exit(await cmd.ClearAsync(formatJson));
        }, jsonOpt);
        ragCmd.AddCommand(ragClearCmd);
        backgroundAgentCmd.AddCommand(ragCmd);

        // nexo background-agent websearch
        var webSearchCmd = new Command("websearch", "Web search configuration and test");
        var webSearchConfigureCmd = new Command("configure", "Show web search configuration");
        webSearchConfigureCmd.SetHandler(async (bool formatJson) =>
        {
            var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.WebSearchCommand>();
            Environment.Exit(await cmd.ConfigureAsync(formatJson));
        }, jsonOpt);
        webSearchCmd.AddCommand(webSearchConfigureCmd);
        var webSearchTestCmd = new Command("test", "Run a test search")
        {
            new Option<string>("--query", () => "Nexo framework", "Search query"),
            new Option<int>("--max-results", () => 5, "Max results")
        };
        webSearchTestCmd.SetHandler(
            async (string query, int maxResults, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.WebSearchCommand>();
                Environment.Exit(await cmd.TestAsync(query, maxResults, formatJson));
            },
            webSearchTestCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            webSearchTestCmd.Options[1] as Option<int> ?? throw new InvalidOperationException(),
            jsonOpt);
        webSearchCmd.AddCommand(webSearchTestCmd);
        backgroundAgentCmd.AddCommand(webSearchCmd);

        // nexo background-agent daemon
        var daemonConfigOpt = new Option<FileInfo?>("--config", "Optional JSON config file with BackgroundAgents:Agents entries.");
        var daemonDurationOpt = new Option<string?>("--duration", "Optional run duration (e.g. 30s, 5m, 1h). Omit to run until Ctrl+C.");
        var daemonPatternStoreOpt = new Option<string?>("--pattern-store-path", "Optional pattern store path override.");
        var daemonDisableObservationOpt = new Option<bool>("--disable-observation", "Disable observation pipeline while running daemon mode.");
        var daemonCmd = new Command("daemon", "Run background agents in long-lived daemon mode")
        {
            daemonConfigOpt,
            daemonDurationOpt,
            daemonPatternStoreOpt,
            daemonDisableObservationOpt
        };
        daemonCmd.SetHandler(
            async (FileInfo? config, string? duration, string? patternStorePath, bool disableObservation, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.BackgroundAgentDaemonCommand>();
                var exitCode = await cmd.RunAsync(
                    config?.FullName,
                    duration,
                    patternStorePath,
                    disableObservation,
                    formatJson);
                Environment.Exit(exitCode);
            },
            daemonConfigOpt,
            daemonDurationOpt,
            daemonPatternStoreOpt,
            daemonDisableObservationOpt,
            jsonOpt);
        backgroundAgentCmd.AddCommand(daemonCmd);

        // nexo background-agent dashboard — localhost read-only operator UI
        var dashboardPortOpt = new Option<int>("--port", () => 5055, "HTTP port (127.0.0.1 only).");
        var dashboardOpenOpt = new Option<bool>("--open", () => false, "Open the default browser to the dashboard URL.");
        var dashboardAuthOpt = new Option<string?>("--auth-token", "Optional shared secret; also read from NEXO_DASHBOARD_AUTH_TOKEN. When set, require ?token= or Bearer header.");
        var dashboardCmd = new Command("dashboard", "Read-only Runtime Studio operator dashboard (objectives, forge, observations)")
        {
            dashboardPortOpt,
            dashboardOpenOpt,
            dashboardAuthOpt
        };
        dashboardCmd.SetHandler(
            async (int port, bool open, string? authToken) =>
            {
                var cmd = ServiceProvider.GetRequiredService<Nexo.CLI.Commands.BackgroundAgent.OperatorDashboardBackgroundAgentCommand>();
                Environment.Exit(await cmd.RunAsync(port, open, authToken, CancellationToken.None));
            },
            dashboardPortOpt,
            dashboardOpenOpt,
            dashboardAuthOpt);
        backgroundAgentCmd.AddCommand(dashboardCmd);

        // nexo trust - Audit and access boundary (Phase 4)
        var trustCmd = new Command("trust", "Trust & Information Architecture: audit log and access boundary");
        var trustAuditCmd = new Command("audit", "Show or export data decision audit log")
        {
            new Option<int>("--count", () => 50, "Max entries to show"),
            new Option<string?>("--since", "Filter by time (e.g. 1h, 30m, or ISO date)"),
            new Option<string?>("--until", "Filter until time (e.g. 1h, 30m, or ISO date)"),
            new Option<string?>("--type", "Filter by event type (Sanitization, BoundaryChange, Classification)"),
            new Option<bool>("--json", "Export as JSON (compliance format)"),
            new Option<bool>("--md", "Export as Markdown"),
            new Option<bool>("--csv", "Export as CSV (compliance)")
        };
        trustAuditCmd.SetHandler(
            async (int count, string? since, string? until, string? type, bool json, bool md, bool csv) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.AuditAsync(count, since, until, type, json, md, csv));
            },
            trustAuditCmd.Options[0] as Option<int> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[4] as Option<bool> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[5] as Option<bool> ?? throw new InvalidOperationException(),
            trustAuditCmd.Options[6] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustAuditCmd);
        var trustPauseCmd = new Command("pause", "Pause observation (halt all data collection)");
        trustPauseCmd.AddOption(jsonOpt);
        trustPauseCmd.SetHandler(
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.PauseAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustPauseCmd);
        var trustResumeCmd = new Command("resume", "Resume observation");
        trustResumeCmd.AddOption(jsonOpt);
        trustResumeCmd.SetHandler(
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ResumeAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustResumeCmd);
        var trustAllowCmd = new Command("allow", "Allow a category or source");
        trustAllowCmd.AddOption(new Option<string?>("--category", "Category to allow (e.g. file-paths, terminal-output)"));
        trustAllowCmd.AddOption(new Option<string?>("--source", "Source to allow (e.g. git, vscode)"));
        trustAllowCmd.AddOption(new Option<string?>("--project", "Project path for override (requires --source)"));
        trustAllowCmd.AddOption(jsonOpt);
        trustAllowCmd.SetHandler(
            async (string? category, string? source, string? project, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.AllowAsync(category, source, project, formatJson));
            },
            trustAllowCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustAllowCmd);
        var trustDenyCmd = new Command("deny", "Deny a category or source");
        trustDenyCmd.AddOption(new Option<string?>("--category", "Category to deny"));
        trustDenyCmd.AddOption(new Option<string?>("--source", "Source to deny"));
        trustDenyCmd.AddOption(new Option<string?>("--project", "Project path for override (requires --source)"));
        trustDenyCmd.AddOption(jsonOpt);
        trustDenyCmd.SetHandler(
            async (string? category, string? source, string? project, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DenyAsync(category, source, project, formatJson));
            },
            trustDenyCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustDenyCmd);
        var trustBoundaryCmd = new Command("boundary", "Show access boundary status");
        trustBoundaryCmd.SetHandler(
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.BoundaryAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustBoundaryCmd);
        var trustDashboardCmd = new Command("dashboard", "Compliance dashboard: boundary status + audit summary");
        trustDashboardCmd.AddOption(new Option<int>("--count", () => 50, "Max audit entries to include"));
        trustDashboardCmd.AddOption(jsonOpt);
        trustDashboardCmd.SetHandler(
            async (int count, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DashboardAsync(count, formatJson));
            },
            trustDashboardCmd.Options[0] as Option<int> ?? throw new InvalidOperationException(),
            trustDashboardCmd.Options[1] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustDashboardCmd);

        var trustPackCmd = new Command("pack", "Manage regulated trust policy packs");
        var trustPackListCmd = new Command("list", "List available trust policy packs");
        trustPackListCmd.AddOption(jsonOpt);
        trustPackListCmd.SetHandler(
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ListPolicyPacksAsync(formatJson));
            },
            jsonOpt);
        trustPackCmd.AddCommand(trustPackListCmd);

        var trustPackDescribeCmd = new Command("describe", "Show rules for a trust policy pack");
        var trustPackDescribeIdArg = new Argument<string>("packId", "Policy pack id");
        trustPackDescribeCmd.AddArgument(trustPackDescribeIdArg);
        trustPackDescribeCmd.AddOption(jsonOpt);
        trustPackDescribeCmd.SetHandler(
            async (string packId, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DescribePolicyPackAsync(packId, formatJson));
            },
            trustPackDescribeIdArg,
            jsonOpt);
        trustPackCmd.AddCommand(trustPackDescribeCmd);

        var trustPackApplyCmd = new Command("apply", "Apply a trust policy pack by id");
        var trustPackIdOpt = new Option<string>("--id", "Policy pack id (strict-enterprise, internal-only, air-gapped)");
        trustPackIdOpt.IsRequired = true;
        trustPackApplyCmd.AddOption(trustPackIdOpt);
        trustPackApplyCmd.AddOption(jsonOpt);
        trustPackApplyCmd.SetHandler(
            async (string id, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ApplyPolicyPackAsync(id, formatJson));
            },
            trustPackIdOpt,
            jsonOpt);
        trustPackCmd.AddCommand(trustPackApplyCmd);
        trustCmd.AddCommand(trustPackCmd);
        root.AddCommand(trustCmd);

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
        var ephemeralOpt = new Option<bool>("--ephemeral", () => false, "Run tests in ephemeral containers; no volume mounts, results discarded when container is removed");
        testCmd.AddOption(coverageOpt);
        testCmd.AddOption(stressOpt);
        testCmd.AddOption(visualOpt);
        testCmd.AddOption(ephemeralOpt);

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
            var ephemeral = ctx.ParseResult.GetValueForOption(ephemeralOpt) || string.Equals(Environment.GetEnvironmentVariable("NEXO_TEST_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            
            var exitCode = await MultiPlatformTestCommand.ExecuteAsync(
                platforms, project, filter, dotnetVersion, executionPlatform, outputDir, coverage, stress, visual, ephemeral, json, verbose, ServiceProvider);
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
                var testCommand = ServiceProvider.GetRequiredService<TestCommand>();
                var exitCode = await testCommand.ExecuteAsync(filter, json, verbose);
                Environment.Exit(exitCode);
            },
            testLocalCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        testCmd.AddCommand(testLocalCmd);
        testCmd.AddCommand(TestPortableCommand.CreateCommand());
        testCmd.AddCommand(TestMultiEnvCommand.CreateCommand());
        testCmd.AddCommand(TestParallelCommand.CreateCommand());

        // nexo orchestrate (resolves lazily - host built only when invoked)
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
        var barrierOpt = new Option<string?>(
            name: "--barrier",
            description: "Barrier level set at request boundary.");
        var jwtOpt = new Option<string?>(
            name: "--jwt",
            description: "Raw bearer JWT used for identity-bound barrier resolution.");
        var headerOpt = new Option<string[]>(
            name: "--header",
            description: "Additional request headers in 'name:value' form.")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var preferredRegionOpt = new Option<string?>(
            name: "--preferred-region",
            description: "Preferred routing region (soft affinity).");

        var orchestrateEphemeralOpt = new Option<bool>("--ephemeral", () => false, "Use ephemeral Ollama container; discarded when command exits");
        orchestrateCmd.AddOption(runtimeSpecOpt);
        orchestrateCmd.AddOption(runtimeSpecJsonOpt);
        orchestrateCmd.AddOption(preferModelOpt);
        orchestrateCmd.AddOption(providerOpt);
        orchestrateCmd.AddOption(barrierOpt);
        orchestrateCmd.AddOption(jwtOpt);
        orchestrateCmd.AddOption(headerOpt);
        orchestrateCmd.AddOption(preferredRegionOpt);
        orchestrateCmd.AddOption(orchestrateEphemeralOpt);

        orchestrateCmd.SetHandler(async context =>
        {
            var request = context.ParseResult.GetValueForArgument(
                orchestrateCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException());
            var runtimeSpec = context.ParseResult.GetValueForOption(runtimeSpecOpt);
            var runtimeSpecJson = context.ParseResult.GetValueForOption(runtimeSpecJsonOpt);
            var preferModel = context.ParseResult.GetValueForOption(preferModelOpt);
            var provider = context.ParseResult.GetValueForOption(providerOpt);
            var barrier = context.ParseResult.GetValueForOption(barrierOpt);
            var jwt = context.ParseResult.GetValueForOption(jwtOpt);
            var rawHeaders = context.ParseResult.GetValueForOption(headerOpt) ?? Array.Empty<string>();
            var preferredRegion = context.ParseResult.GetValueForOption(preferredRegionOpt);
            var ephemeral = context.ParseResult.GetValueForOption(orchestrateEphemeralOpt);
            var json = context.ParseResult.GetValueForOption(jsonOpt);
            var verbose = context.ParseResult.GetValueForOption(verboseOpt);

            if (ephemeral || string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase))
                Environment.SetEnvironmentVariable("NEXO_EPHEMERAL_MODELS", "1");

            using var scope = ServiceProvider.CreateScope();
            var orchestrateCommand = scope.ServiceProvider.GetRequiredService<OrchestrateCommand>();
            var headers = ParseHeaders(rawHeaders);
            var exitCode = await orchestrateCommand.ExecuteAsync(
                request,
                runtimeSpec?.FullName,
                runtimeSpecJson,
                preferModel,
                provider,
                barrier,
                preferredRegion,
                json,
                verbose,
                jwt,
                headers);
            Environment.Exit(exitCode);
        });
        root.AddCommand(orchestrateCmd);

        // nexo pipeline
        var pipelineCmd = new Command("pipeline", "Validate and run pipeline templates.");

        var pipelineValidateCmd = new Command("validate", "Validate a pipeline template file.");
        var pipelineValidateTemplateOpt = new Option<FileInfo>("--template", "Pipeline template JSON file") { IsRequired = true };
        pipelineValidateCmd.AddOption(pipelineValidateTemplateOpt);
        pipelineValidateCmd.SetHandler(
            (FileInfo template, bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<PipelineCommand>();
                var exitCode = cmd.Validate(template.FullName, json, verbose);
                Environment.Exit(exitCode);
            },
            pipelineValidateTemplateOpt,
            jsonOpt,
            verboseOpt);
        pipelineCmd.AddCommand(pipelineValidateCmd);

        var pipelineRunCmd = new Command("run", "Run a pipeline template file.");
        var pipelineRunTemplateOpt = new Option<FileInfo>("--template", "Pipeline template JSON file") { IsRequired = true };
        var pipelineRunIdOpt = new Option<string?>("--run-id", "Optional explicit run id");
        var pipelineResumeRunIdOpt = new Option<string?>("--resume-run-id", "Optional prior run id to resume from");
        var pipelineResumeFailedOpt = new Option<bool>("--resume-failed-stages", () => false, "Resume only failed stages from prior run");
        var pipelineInputOpt = new Option<string[]>("--input", "Key/value input in key=value format")
        {
            AllowMultipleArgumentsPerToken = true
        };
        pipelineRunCmd.AddOption(pipelineRunTemplateOpt);
        pipelineRunCmd.AddOption(pipelineRunIdOpt);
        pipelineRunCmd.AddOption(pipelineResumeRunIdOpt);
        pipelineRunCmd.AddOption(pipelineResumeFailedOpt);
        pipelineRunCmd.AddOption(pipelineInputOpt);
        pipelineRunCmd.SetHandler(
            async (FileInfo template, string? runId, string? resumeRunId, bool resumeFailedStages, string[] rawInputs, bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<PipelineCommand>();
                var inputs = ParsePipelineInputs(rawInputs);
                var exitCode = await cmd.RunAsync(
                    templatePath: template.FullName,
                    json: json,
                    verbose: verbose,
                    runId: runId,
                    resumeRunId: resumeRunId,
                    resumeFailedStages: resumeFailedStages,
                    inputs: inputs,
                    cancellationToken: CancellationToken.None);
                Environment.Exit(exitCode);
            },
            pipelineRunTemplateOpt,
            pipelineRunIdOpt,
            pipelineResumeRunIdOpt,
            pipelineResumeFailedOpt,
            pipelineInputOpt,
            jsonOpt,
            verboseOpt);
        pipelineCmd.AddCommand(pipelineRunCmd);

        var pipelineDiagnosticsCmd = new Command("diagnostics", "Show resolved pipeline runtime configuration.");
        pipelineDiagnosticsCmd.SetHandler(
            (bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<PipelineCommand>();
                var exitCode = cmd.Diagnostics(json);
                Environment.Exit(exitCode);
            },
            jsonOpt);
        pipelineCmd.AddCommand(pipelineDiagnosticsCmd);
        root.AddCommand(pipelineCmd);

        // nexo escalate (resolves lazily)
        var escalateCmd = new Command("escalate", "Manage escalations and conflicts");
        
        // nexo escalate list
        var escalateListCmd = new Command("list", "List all pending escalations");
        escalateListCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<EscalateCommand>();
                var exitCode = await cmd.ListAsync(json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<EscalateCommand>();
                var exitCode = await cmd.ShowAsync(id, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<EscalateCommand>();
                var exitCode = await cmd.ResolveAsync(id, resolution, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<EscalateCommand>();
                var exitCode = await cmd.DismissAsync(id, reason, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<EscalateCommand>();
                var exitCode = await cmd.ListBySeverityAsync(severity, json, verbose);
                Environment.Exit(exitCode);
            },
            escalateListBySeverityCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt,
            verboseOpt);
        escalateCmd.AddCommand(escalateListBySeverityCmd);

        // nexo metrics (resolves lazily)
        var metricsCmd = new Command("metrics", "View orchestration metrics and performance data");
        
        // nexo metrics report
        var metricsReportCmd = new Command("report", "Show performance report");
        metricsReportCmd.SetHandler(
            async (bool json, bool verbose) =>
            {
                var cmd = ServiceProvider.GetRequiredService<MetricsCommand>();
                var exitCode = await cmd.ShowReportAsync(json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<MetricsCommand>();
                var exitCode = await cmd.ShowAgentAsync(id, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<MetricsCommand>();
                var exitCode = await cmd.ShowTracesAsync(correlationId, operation, json, verbose);
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
                var cmd = ServiceProvider.GetRequiredService<MetricsCommand>();
                var exitCode = await cmd.ClearAsync(json, verbose);
                Environment.Exit(exitCode);
            },
            jsonOpt,
            verboseOpt);
        metricsCmd.AddCommand(metricsClearCmd);

        // nexo metrics self-improvement (P3.4: holdout pass rate)
        var metricsSelfImprovementCmd = new Command("self-improvement", "Show self-improvement metrics (holdout pass rate, etc.)");
        metricsSelfImprovementCmd.SetHandler(
            async (bool json) =>
            {
                var store = ServiceProvider.GetRequiredService<Nexo.Core.Application.SelfImprovement.Ports.ISelfImprovementMetricsStore>();
                var report = await store.GetLastAsync();
                if (report == null)
                {
                    if (json) Console.WriteLine("{\"ok\":true,\"message\":\"No self-improvement run recorded\"}");
                    else Console.WriteLine("No self-improvement run recorded. Run 'nexo improve --self' first.");
                    return;
                }
                if (json)
                {
                    var j = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ok = true,
                        runAt = report.RunAt,
                        fixesPromoted = report.FixesPromoted,
                        holdoutPassed = report.HoldoutPassed,
                        holdoutTotal = report.HoldoutTotal,
                        holdoutPassRate = report.HoldoutPassRate
                    }, j));
                }
                else
                {
                    Console.WriteLine($"Last run: {report.RunAt:O}");
                    Console.WriteLine($"  Fixes promoted: {report.FixesPromoted}");
                    if (report.HoldoutTotal.HasValue)
                    {
                        Console.WriteLine($"  Holdout: {report.HoldoutPassed}/{report.HoldoutTotal} passed");
                        if (report.HoldoutPassRate.HasValue)
                            Console.WriteLine($"  Holdout pass rate: {report.HoldoutPassRate:P1}");
                    }
                }
            },
            jsonOpt);
        metricsCmd.AddCommand(metricsSelfImprovementCmd);

        // nexo docker (generic build/run/clean for multi-platform testing)
        var dockerCmd = new DockerCommand();
        root.AddCommand(dockerCmd);

        // nexo maintenance clean
        var maintenanceCmd = new Command("maintenance", "Artifact cleanup and maintenance");
        var maintenanceCleanCmd = new Command("clean", "Clean disk artifacts (test-artifacts, incomplete-blobs)");
        var strategyOpt = new Option<string?>("--strategy", "Strategy ID (test-artifacts, incomplete-blobs); omit for all");
        var repoOpt = new Option<string?>("--repo", "Repository root for context");
        maintenanceCleanCmd.AddOption(strategyOpt);
        maintenanceCleanCmd.AddOption(repoOpt);
        maintenanceCleanCmd.SetHandler(
            async (string? strategy, string? repo, bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<MaintenanceCommand>();
                var exitCode = await cmd.ExecuteAsync(strategy, repo, json);
                Environment.Exit(exitCode);
            },
            strategyOpt,
            repoOpt,
            jsonOpt);
        maintenanceCmd.AddCommand(maintenanceCleanCmd);
        root.AddCommand(maintenanceCmd);

        root.AddCommand(new CiCommand());
        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);
        root.AddCommand(configCmd);
        root.AddCommand(new DogfoodCommand());
        root.AddCommand(new BootstrapCommand());
        root.AddCommand(new DoctorCommand());
        root.AddCommand(new RuntimeCommand());
        root.AddCommand(new WorkflowCommand(() => ServiceProvider.CreateScope()));
        root.AddCommand(new RuntimeStudioCommand());
        root.AddCommand(new ChatCommand(() => ServiceProvider.GetRequiredService<OrchestrateCommand>()));
        root.AddCommand(new SelfExtendCommand(
            () => ServiceProvider.GetRequiredService<Nexo.BackgroundAgents.HostRunners.SelfExtendRunnerAdapter>()));
        root.AddCommand(new ObserveCommand());
        root.AddCommand(new AdaptCommand());
        root.AddCommand(new ImproveCommand());
        root.AddCommand(new IngestFailuresCommand());
        root.AddCommand(new SelfContextCommand());
        root.AddCommand(new ChangelogCommand());
        root.AddCommand(new RollbackCommand());
        root.AddCommand(new ComposeCommand());
        root.AddCommand(new MeshCommand());
        root.AddCommand(backgroundAgentCmd);
        root.AddCommand(testCmd);
        root.AddCommand(escalateCmd);
        root.AddCommand(metricsCmd);

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
