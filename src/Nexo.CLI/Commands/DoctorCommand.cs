using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>
/// Aggregated onboarding doctor command with one pass/fail report.
/// </summary>
public sealed class DoctorCommand : Command
{
    public DoctorCommand() : base("doctor", "Run onboarding readiness report (dependencies + CLI/runtime checks).")
    {
        var includeOptionalOpt = new Option<bool>(
            "--include-optional",
            () => false,
            "Include optional dependencies in required checks.");
        var profileOpt = new Option<string>(
            "--profile",
            () => "demo",
            "Doctor profile: demo | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        var fixOpt = new Option<bool>("--fix", () => false, "Attempt safe remediation for fixable onboarding failures.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "With --fix, list remediation actions without running them.");
        var yesOpt = new Option<bool>("--yes", () => false, "Auto-approve remediation actions when --fix is enabled.");

        AddOption(includeOptionalOpt);
        AddOption(profileOpt);
        AddOption(jsonOpt);
        AddOption(fixOpt);
        AddOption(dryRunOpt);
        AddOption(yesOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var fix = ctx.ParseResult.GetValueForOption(fixOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            ctx.ExitCode = await ExecuteAsync(profile, includeOptional, json, fix, dryRun, yes, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
    }

    internal static async Task<int> ExecuteAsync(
        string profile,
        bool includeOptional,
        bool json,
        bool fix,
        bool dryRun,
        bool autoApproveFixes,
        CancellationToken ct)
    {
        var dependencyAssessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        var hostOs = RuntimeInformation.OSDescription;
        var osSupported = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
        var dependencyOk = dependencyAssessment.Supported && !dependencyAssessment.MissingRequired.Any();
        var cliCommand = "dotnet run --project src/Nexo.CLI -- --help";
        var containerCommand = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help";

        var (cliExitCode, _, cliStderr) = await RunShellCaptureAsync(cliCommand, ct).ConfigureAwait(false);
        var cliSmokePassed = cliExitCode == 0;
        var cliSmokeError = cliSmokePassed ? string.Empty : cliStderr.Trim();

        var (containerExitCode, _, containerStderr) = await RunShellCaptureAsync(containerCommand, ct).ConfigureAwait(false);
        var containerSmokePassed = containerExitCode == 0;
        var containerSmokeError = containerSmokePassed ? string.Empty : containerStderr.Trim();

        var overallOk = osSupported && dependencyOk && cliSmokePassed;
        var remediation = new DoctorRemediationReport();
        if (fix)
        {
            if (dryRun && !json)
                Console.WriteLine("doctor --fix --dry-run: planned remediation (no commands executed):");

            remediation = await DoctorRemediation.RunAsync(
                dependencyAssessment,
                osSupported,
                cliSmokePassed,
                profile,
                includeOptional,
                autoApproveFixes,
                json,
                dryRun,
                ct).ConfigureAwait(false);

            if (!dryRun)
            {
                var postAssessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
                dependencyAssessment = postAssessment;
                dependencyOk = postAssessment.Supported && !postAssessment.MissingRequired.Any();

                var (postCliExitCode, _, postCliStderr) = await RunShellCaptureAsync(cliCommand, ct).ConfigureAwait(false);
                cliSmokePassed = postCliExitCode == 0;
                cliSmokeError = cliSmokePassed ? string.Empty : postCliStderr.Trim();
                overallOk = osSupported && dependencyOk && cliSmokePassed;
            }
        }

        if (json)
        {
            var payload = new
            {
                ok = overallOk,
                profile,
                hostOs,
                checks = new
                {
                    osSupported,
                    dependencyCheck = new
                    {
                        supported = dependencyAssessment.Supported,
                        missingRequired = dependencyAssessment.MissingRequired.Select(m => m.Id).ToArray(),
                        includeOptional
                    },
                    cliSmoke = new
                    {
                        passed = cliSmokePassed,
                        command = cliCommand,
                        error = cliSmokeError
                    },
                    containerSmoke = new
                    {
                        passed = containerSmokePassed,
                        command = containerCommand,
                        error = containerSmokeError
                    },
                    remediation = remediation
                },
                nextSteps = new
                {
                    nativeInstall = "bash scripts/install/install.sh --yes",
                    containerRun = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help",
                    doctorFix = "dotnet run --project src/Nexo.CLI -- doctor --fix --yes"
                }
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"doctor profile: {profile}");
            Console.WriteLine($"host os: {hostOs}");
            Console.WriteLine($"os supported: {(osSupported ? "yes" : "no")}");
            Console.WriteLine($"dependency check: {(dependencyOk ? "pass" : "fail")}");
            if (!dependencyOk)
            {
                Console.WriteLine($"missing required dependencies: {string.Join(", ", dependencyAssessment.MissingRequired.Select(m => m.DisplayName))}");
            }

            Console.WriteLine($"cli smoke: {(cliSmokePassed ? "pass" : "fail")}");
            if (!cliSmokePassed && !string.IsNullOrWhiteSpace(cliSmokeError))
                Console.WriteLine($"  cli smoke error: {cliSmokeError}");

            Console.WriteLine($"container smoke: {(containerSmokePassed ? "pass" : "warn")}");
            if (!containerSmokePassed && !string.IsNullOrWhiteSpace(containerSmokeError))
                Console.WriteLine($"  container smoke error: {containerSmokeError}");

            if (fix)
            {
                Console.WriteLine(dryRun ? "remediation (dry run):" : "remediation:");
                if (remediation.Attempts.Count == 0)
                {
                    Console.WriteLine("  no fixable issues were detected.");
                }
                else
                {
                    foreach (var attempt in remediation.Attempts)
                    {
                        var outcomeLabel = dryRun
                            ? "would-run"
                            : attempt.Success ? "fixed" : "not-fixed";
                        Console.WriteLine(
                            $"  - {attempt.Id}: {outcomeLabel} ({attempt.Status})");
                        if (!string.IsNullOrWhiteSpace(attempt.Command))
                            Console.WriteLine($"      command: {attempt.Command}");
                        if (!string.IsNullOrWhiteSpace(attempt.Message))
                            Console.WriteLine($"      {attempt.Message}");
                    }
                }
            }

            Console.WriteLine($"overall: {(overallOk ? "PASS" : "FAIL")}");
            Console.WriteLine("recommended next steps:");
            Console.WriteLine("  - native lane: bash scripts/install/install.sh --yes");
            Console.WriteLine("  - container lane: docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help");
            Console.WriteLine("  - remediation lane: dotnet run --project src/Nexo.CLI -- doctor --fix --yes");
        }

        return overallOk ? 0 : 1;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunShellCaptureAsync(string command, CancellationToken ct)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "powershell" : "bash",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (isWindows)
        {
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(command);
        }
        else
        {
            psi.ArgumentList.Add("-lc");
            psi.ArgumentList.Add(command);
        }

        using var process = Process.Start(psi);
        if (process == null)
            return (1, string.Empty, "Failed to start process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return (process.ExitCode, stdout, stderr);
    }
}
