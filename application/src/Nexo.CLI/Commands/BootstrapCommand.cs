using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>CLI command for bootstrap.</summary>
public sealed class BootstrapCommand : Command
{
    /// <summary>Creates a new BootstrapCommand instance.</summary>
    public BootstrapCommand() : base("bootstrap", "Cross-platform environment bootstrap (check/install dependencies)")
    {
        /// <summary>Add alias.</summary>
        AddAlias("doctor-legacy");
        var includeOptionalOpt = new Option<bool>(
            "--include-optional",
            () => false,
            "Include optional dependencies (docker, ollama) in checks/install plan.");
        var profileOpt = new Option<string>(
            "--profile",
            () => "demo",
            "Bootstrap profile: demo | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        var yesOpt = new Option<bool>("--yes", () => false, "Auto-approve install plan.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Show install plan without executing commands.");

        var checkCmd = new Command("check", "Check local machine readiness for the demo profile.");
        checkCmd.AddOption(includeOptionalOpt);
        checkCmd.AddOption(profileOpt);
        checkCmd.AddOption(jsonOpt);
        checkCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            ctx.ExitCode = await RunCheckAsync(profile, includeOptional, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(checkCmd);

        var applyCmd = new Command("apply", "Install missing dependencies for the demo profile.");
        applyCmd.AddOption(includeOptionalOpt);
        applyCmd.AddOption(profileOpt);
        applyCmd.AddOption(jsonOpt);
        applyCmd.AddOption(yesOpt);
        applyCmd.AddOption(dryRunOpt);
        applyCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            ctx.ExitCode = await RunApplyAsync(profile, includeOptional, yes, dryRun, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(applyCmd);

        // Default command behavior: `nexo bootstrap` == `nexo bootstrap check`
        AddOption(includeOptionalOpt);
        /// <summary>Add option.</summary>
        AddOption(profileOpt);
        /// <summary>Add option.</summary>
        AddOption(jsonOpt);
        this.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            ctx.ExitCode = await RunCheckAsync(profile, includeOptional, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
    }

    internal static async Task<int> RunCheckAsync(string profile, bool includeOptional, bool json, CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        BootstrapRuntime.RenderAssessment(assessment, json);
        return 0;
    }

    internal static async Task<int> RunApplyAsync(string profile, bool includeOptional, bool yes, bool dryRun, bool json, CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        if (!assessment.Supported)
        {
            BootstrapRuntime.RenderAssessment(assessment, json);
            return 1;
        }

        var plan = BootstrapRuntime.BuildInstallPlan(assessment);
        if (plan.Count == 0)
        {
            BootstrapRuntime.RenderAssessment(assessment, json);
            if (!json)
                Console.WriteLine("Bootstrap apply: nothing to install.");
            return 0;
        }

        if (!yes && !dryRun)
        {
            Console.WriteLine("Install plan:");
            foreach (var dep in plan)
                Console.WriteLine($"  - {dep.DisplayName}: {dep.InstallCommand}");

            Console.Write("Proceed with installation? [y/N]: ");
            var answer = Console.ReadLine();
            if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelled.");
                return 130;
            }
        }

        if (dryRun)
        {
            if (json)
            {
                var payload = new
                {
                    ok = true,
                    dryRun = true,
                    steps = plan.Select(p => new { id = p.Id, name = p.DisplayName, install = p.InstallCommand }).ToArray(),
                };
                Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("Dry run install plan:");
                foreach (var dep in plan)
                    Console.WriteLine($"  - {dep.DisplayName}: {dep.InstallCommand}");
            }
            return 0;
        }

        var failures = new List<string>();
        foreach (var dep in plan)
        {
            Console.WriteLine($"Installing {dep.DisplayName}...");
            var exitCode = await BootstrapRuntime.RunShellStreamingAsync(dep.InstallCommand, ct).ConfigureAwait(false);
            if (exitCode != 0)
                failures.Add($"{dep.DisplayName} (exit {exitCode})");
        }

        var post = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        var success = !post.MissingRequired.Any() && failures.Count == 0;

        if (json)
        {
            var payload = new
            {
                ok = success,
                installed = plan.Select(p => p.Id).ToArray(),
                failures,
                missingRequiredAfterApply = post.MissingRequired.Select(m => m.Id).ToArray(),
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            if (failures.Count > 0)
                Console.WriteLine($"Install failures: {string.Join(", ", failures)}");
            if (post.MissingRequired.Any())
                Console.WriteLine($"Still missing required dependencies: {string.Join(", ", post.MissingRequired.Select(m => m.DisplayName))}");
            Console.WriteLine(success ? "Bootstrap apply completed successfully." : "Bootstrap apply completed with errors.");
        }

        return success ? 0 : 1;
    }
}
