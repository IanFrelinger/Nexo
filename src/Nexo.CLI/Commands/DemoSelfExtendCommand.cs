using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Demo.SelfExtend;
using Nexo.CLI.Demo.SelfExtend.Pipeline;
using Nexo.CLI.Output;
using Nexo.Core.Application.Host;
using Nexo.Core.Application.Interfaces;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Demo: show how the framework can extend itself using a configurable agent + TDD loop.
/// The agent generates a new demo command and a test, runs tests, fixes implementation, re-runs tests.
/// </summary>
public sealed class DemoSelfExtendCommand : Command
{
    public DemoSelfExtendCommand() : base("self-extend", "Demo self-extension: generate a new demo command via iterative TDD")
    {
        var nameOption = new Option<string>(
            "--command",
            () => "hello-gen-" + Guid.NewGuid().ToString("N")[..8],
            "Command name to generate under `nexo demo` (e.g. hello-gen)");

        var messageOption = new Option<string>(
            "--message",
            () => "hello from generated command",
            "Message the generated command should output");

        var maxIterationsOption = new Option<int>(
            "--max-phases",
            () => 3,
            "Maximum planning iterations (each iteration may include multiple pipeline commands)");

        var verboseOption = new Option<bool>(
            "--verbose",
            () => false,
            "Show detailed progress");

        var jsonOption = new Option<bool>(
            "--format-json",
            () => false,
            "Emit machine-readable JSON");

        var specPathOption = new Option<string?>(
            "--spec",
            () => null,
            "Runtime spec JSON path (controls planner + step implementations)");

        var specJsonOption = new Option<string?>(
            "--spec-json",
            () => null,
            "Runtime spec JSON string (controls planner + step implementations)");

        var providerOption = new Option<string?>(
            "--provider",
            () => null,
            "Override LLM provider for LLM steps (openai/azure/ollama/mock-json/offline)");

        var preferOption = new Option<string?>(
            "--prefer",
            () => null,
            "Override implementation preference (llm|deterministic)");

        AddOption(nameOption);
        AddOption(messageOption);
        AddOption(maxIterationsOption);
        AddOption(verboseOption);
        AddOption(jsonOption);
        AddOption(specPathOption);
        AddOption(specJsonOption);
        AddOption(providerOption);
        AddOption(preferOption);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var cmdName = ctx.ParseResult.GetValueForOption(nameOption)!;
            var message = ctx.ParseResult.GetValueForOption(messageOption)!;
            var maxIterations = ctx.ParseResult.GetValueForOption(maxIterationsOption);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
            var json = ctx.ParseResult.GetValueForOption(jsonOption);
            var specPath = ctx.ParseResult.GetValueForOption(specPathOption);
            var specJson = ctx.ParseResult.GetValueForOption(specJsonOption);
            var provider = ctx.ParseResult.GetValueForOption(providerOption);
            var prefer = ctx.ParseResult.GetValueForOption(preferOption);

            await ExecuteAsync(cmdName, message, maxIterations, verbose, json, specPath, specJson, provider, prefer, ctx.GetCancellationToken());
        });
    }

    private static async Task ExecuteAsync(
        string commandName,
        string message,
        int maxIterations,
        bool verbose,
        bool json,
        string? specPath,
        string? specJson,
        string? providerOverride,
        string? preferOverride,
        CancellationToken ct)
    {
        var repoRoot = Environment.CurrentDirectory;
        var console = json ? null : new CliConsole(verbose);

        var spec = new SelfExtendSpec
        {
            CommandName = commandName,
            Message = message
        };

        if (!json && console != null)
        {
            console.WriteHeader("🧬 Self-Extension Demo (TDD)");
            console.WritePair("Repo root", repoRoot);
            console.WritePair("Generated command", $"nexo demo {spec.CommandName}");
            console.WritePair("Expected message", spec.Message);
            console.WritePair("Max iterations", maxIterations.ToString());
            console.WriteLine();
        }

        var runtimeCfg = SelfExtendConfigLoader.Load(specPath, specJson);
        if (!string.IsNullOrWhiteSpace(providerOverride))
        {
            runtimeCfg = runtimeCfg with { Provider = providerOverride!.Trim() };
        }
        if (!string.IsNullOrWhiteSpace(preferOverride))
        {
            runtimeCfg = runtimeCfg with { Prefer = preferOverride!.Trim() };
        }

        var toolRuntime = new SelfExtendToolRuntime();

        // Use the core aggregator/orchestrator pattern.
        var sp = ServiceHost.BuildDefault(services =>
        {
            services.AddLogging();
            services.AddSingleton<IProviderFactory, ProviderFactory>();
            services.AddSingleton<Nexo.Core.Application.Orchestration.IPreValidator, SelfExtendPreValidator>();
        });
        var orchestrator = sp.GetRequiredService<IOrchestrator>();
        var providerFactory = sp.GetRequiredService<IProviderFactory>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        var stepFactory = new SelfExtendStepFactory(toolRuntime, providerFactory, loggerFactory, runtimeCfg);
        var planner = new ConfigurableSelfExtendPlanner(
            runtimeCfg,
            stepFactory,
            providerFactory,
            loggerFactory.CreateLogger<ConfigurableSelfExtendPlanner>());

        var ctxObj = new Nexo.CLI.Demo.SelfExtend.Pipeline.SelfExtendContext
        {
            RepoRoot = repoRoot,
            OutputRoot = Path.Combine(repoRoot, "out"),
            Spec = spec
        };

        var overallOk = true;
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            ctxObj.Iteration = iteration;
            var plan = planner.Next(ctxObj);
            if (plan.Count == 0) break;

            if (!json && console != null)
            {
                console.WriteHeader($"Iteration {iteration}");
            }

            foreach (var step in plan)
            {
                if (!json && console != null) console.StartSpinner($"Running step: {step.GetType().Name}");
                OrchestrationResult result;
                try
                {
                    result = await orchestrator.RunAsync(step, ctxObj, ct);
                }
                catch (Exception ex)
                {
                    overallOk = false;
                    ctxObj.LastTestOk = false;
                    ctxObj.History.Add(new { iteration, step = step.GetType().Name, ok = false, exception = ex.Message });
                    if (!json && console != null) console.WriteError($"{step.GetType().Name} threw: {ex.Message}");
                    break;
                }
                finally
                {
                    if (!json && console != null) console.StopSpinner();
                }

                if (!result.Success)
                {
                    overallOk = false;
                    ctxObj.LastTestOk = false;
                    ctxObj.History.Add(new { iteration, step = step.GetType().Name, ok = false, error = result.Message });
                    if (!json && console != null) console.WriteError($"{step.GetType().Name} failed: {result.Message}");
                    break;
                }
                else
                {
                    ctxObj.History.Add(new { iteration, step = step.GetType().Name, ok = true });
                    if (!json && console != null) console.WriteSuccess(step.GetType().Name);
                }
            }
        }

        overallOk = overallOk && ctxObj.LastTestOk == true;

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = overallOk,
                command = ctxObj.GeneratedCommandInvocation,
                message = spec.Message,
                lastTestOk = ctxObj.LastTestOk,
                history = ctxObj.History
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (console != null)
        {
            console.WriteLine();
            if (overallOk)
            {
                console.WriteSuccess($"Demo complete. Try: `dotnet run --project src/Nexo.CLI -- {ctxObj.GeneratedCommandInvocation}`");
            }
            else
            {
                console.WriteError("Demo failed to verify the generated command contract.");
            }
            console.WriteWarning("Generated files are placed under `src/.../DemoGenerated/` and ignored by git (demo artifacts).");
        }
    }
}

