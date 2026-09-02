using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ashlar.CLI.Commands;

/// <summary>
/// Aggregated onboarding doctor command with one pass/fail report.
/// </summary>
public sealed class DoctorCommand : Command
{
    /// <summary>Creates a new DoctorCommand instance.</summary>
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
        var pathOpt = new Option<DirectoryInfo>(
            "--path",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Project directory to also report readiness for (defaults to current). Ignored when not an ashlar project.");

        AddOption(includeOptionalOpt);
        AddOption(profileOpt);
        AddOption(jsonOpt);
        AddOption(fixOpt);
        AddOption(dryRunOpt);
        AddOption(yesOpt);
        AddOption(pathOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var fix = ctx.ParseResult.GetValueForOption(fixOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            var path = ctx.ParseResult.GetValueForOption(pathOpt)?.FullName;
            ctx.ExitCode = await ExecuteAsync(profile, includeOptional, json, fix, dryRun, yes, ctx.GetCancellationToken(), path).ConfigureAwait(false);
        });
    }

    internal static async Task<int> ExecuteAsync(
        string profile,
        bool includeOptional,
        bool json,
        bool fix,
        bool dryRun,
        bool autoApproveFixes,
        CancellationToken ct,
        string? projectPath = null)
    {
        // Project readiness is a SEPARATE question from environment health, reported alongside it
        // but never folded into the environment exit code. It is null when the path is not an
        // ashlar project, and the block is then omitted entirely.
        var readiness = DoctorProjectReadiness.Assess(projectPath ?? Environment.CurrentDirectory);

        var dependencyAssessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        var hostOs = RuntimeInformation.OSDescription;
        var osSupported = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
        var dependencyOk = dependencyAssessment.Supported && !dependencyAssessment.MissingRequired.Any();

        // The CLI smoke test builds the CLI FROM A REPO CHECKOUT. Outside one — which is the
        // supported external path, `dotnet tool install --global Ashlar.CLI` — there is no project
        // to build, and running it anyway produced "The provided file path does not exist" and a
        // permanent overall FAIL with exit 1. The first diagnostic a new user runs told them their
        // installation was broken when it was fine.
        //
        // A check that cannot apply here reports NOT APPLICABLE and is excluded from the verdict.
        // It is never silently treated as a pass: the tri-state below has no true branch for a
        // check that did not run, the JSON carries passed:null beside applicable:false, and the
        // text output prints the reason. A skipped check that reads as green is the failure mode
        // this whole pass exists to remove.
        var containerCommand = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help";
        var repoCliProject = FindRepoCliProject();
        var cliApplicable = repoCliProject is not null;
        var cliCommand = cliApplicable
            ? $"dotnet run --project \"{repoCliProject}\" -- --help"
            : "dotnet run --project application/src/Ashlar.CLI -- --help";
        var cliNotApplicableReason = "no Ashlar repo checkout above the current directory — "
            + "this lane builds the CLI from source, and there is no source here. "
            + "The CLI you are running (installed as a dotnet tool, or from a bundle) is what produced this report.";

        bool? cliSmokePassed = null;
        var cliSmokeError = string.Empty;
        if (cliApplicable)
        {
            var (cliExitCode, _, cliStderr) = await RunShellCaptureAsync(cliCommand, ct).ConfigureAwait(false);
            cliSmokePassed = cliExitCode == 0;
            cliSmokeError = cliSmokePassed == true ? string.Empty : cliStderr.Trim();
        }

        var (containerExitCode, _, containerStderr) = await RunShellCaptureAsync(containerCommand, ct).ConfigureAwait(false);
        var containerSmokePassed = containerExitCode == 0;
        var containerSmokeError = containerSmokePassed ? string.Empty : containerStderr.Trim();

        // `cliSmokePassed != false` — not `== true`. A check that did not apply cannot fail the
        // verdict, and cannot pass it either; it is absent from the judgement and said so above.
        var overallOk = osSupported && dependencyOk && cliSmokePassed != false;
        var remediation = new DoctorRemediationReport();
        if (fix)
        {
            if (dryRun && !json)
                Console.WriteLine("doctor --fix --dry-run: planned remediation (no commands executed):");

            remediation = await DoctorRemediation.RunAsync(
                dependencyAssessment,
                osSupported,
                // `?? true` means "nothing to remediate", not "the lane is healthy". Remediation
                // reads this flag for exactly one decision — whether to offer `dotnet build
                // application/src/Ashlar.CLI/Ashlar.CLI.csproj` — and offering that to someone with
                // no repo checkout is the second half of the defect this lane's tri-state removes:
                // they were told their install had failed and then handed a command that cannot
                // run. `false` here would do precisely that. The verdict is computed separately,
                // above, where a check that did not apply is excluded rather than assumed to pass.
                cliSmokePassed ?? true,
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

                if (cliApplicable)
                {
                    var (postCliExitCode, _, postCliStderr) = await RunShellCaptureAsync(cliCommand, ct).ConfigureAwait(false);
                    cliSmokePassed = postCliExitCode == 0;
                    cliSmokeError = cliSmokePassed == true ? string.Empty : postCliStderr.Trim();
                }
                overallOk = osSupported && dependencyOk && cliSmokePassed != false;
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
                        // passed is null when applicable is false. A consumer that reads `passed`
                        // as a boolean gets null, not true — a skipped check must never deserialize
                        // into a green one.
                        applicable = cliApplicable,
                        passed = cliSmokePassed,
                        command = cliApplicable ? cliCommand : null,
                        notApplicableReason = cliApplicable ? null : cliNotApplicableReason,
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
                projectReadiness = readiness is null ? null : new
                {
                    verdict = readiness.Verdict,
                    ready = readiness.Ready,
                    verified = readiness.Verified,
                    key = readiness.KeyStatus,
                    fingerprint = readiness.Fingerprint,
                    ledger = readiness.LedgerStatus,
                    ledgerEntries = readiness.LedgerEntries,
                    nextStep = readiness.NextStep
                },
                nextSteps = cliApplicable
                    ? new
                    {
                        devContainer = "Open repo in Cursor/VS Code → Dev Containers: Reopen in Container",
                        containerRun = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help",
                        doctorFix = "dotnet run --project application/src/Ashlar.CLI -- doctor --fix --yes"
                    }
                    : new
                    {
                        // Outside a checkout the repo-lane instructions are not just inapplicable,
                        // they are misleading: they are what a tool user was told to run after
                        // being told their install had failed.
                        devContainer = "Not needed for a tool install. Clone the repo only if you want the source lanes.",
                        containerRun = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help",
                        doctorFix = "ashlar doctor --fix --yes"
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

            Console.WriteLine($"cli smoke: {cliSmokePassed switch { true => "pass", false => "fail", null => "n/a — does not apply here" }}");
            if (!cliApplicable)
                Console.WriteLine($"  {cliNotApplicableReason}");
            if (cliSmokePassed == false && !string.IsNullOrWhiteSpace(cliSmokeError))
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

            Console.WriteLine($"overall: {(overallOk ? "PASS" : "FAIL")}"
                + (cliApplicable ? string.Empty : "  (1 check did not apply and was excluded — see cli smoke above)"));
            Console.WriteLine("recommended next steps:");
            if (cliApplicable)
            {
                Console.WriteLine("  - dev container: Reopen in Container (.devcontainer/)");
                Console.WriteLine("  - container lane: docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help");
                Console.WriteLine("  - remediation lane: dotnet run --project application/src/Ashlar.CLI -- doctor --fix --yes");
            }
            else
            {
                Console.WriteLine("  - you are running the CLI outside a repo checkout, which is the supported external path.");
                Console.WriteLine("  - container lane: docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help");
                Console.WriteLine("  - remediation lane: ashlar doctor --fix --yes");
                Console.WriteLine("  - source lanes: clone the repo and re-run doctor from inside it to exercise them.");
            }

            if (readiness is not null)
            {
                Console.WriteLine();
                Console.WriteLine($"project readiness: {readiness.Verdict}");
                Console.WriteLine($"  verify: {(readiness.Verified ? "pass" : "fail")}");
                Console.WriteLine($"  operator key: {readiness.KeyStatus}"
                    + (readiness.Fingerprint is { } fp ? $" ({fp})" : string.Empty));
                Console.WriteLine($"  ledger: {readiness.LedgerStatus}"
                    + (readiness.LedgerStatus == "intact" ? $" ({readiness.LedgerEntries} signed)" : string.Empty));
                Console.WriteLine($"  next: {readiness.NextStep}");
            }
        }

        return overallOk ? 0 : 1;
    }

    /// <summary>
    /// The repo checkout's <c>Ashlar.CLI.csproj</c>, or null when there is no checkout here.
    ///
    /// <para>Searched from the current directory upwards and then from the running assembly
    /// upwards — a developer may run <c>ashlar</c> from a subdirectory of the repo, and a
    /// build-output run sits under the checkout without the current directory being in it. Both are
    /// a real checkout; neither is present for a <c>dotnet tool</c> install, which is exactly the
    /// distinction the caller needs.</para>
    /// </summary>
    internal static string? FindRepoCliProject() =>
        FindRepoCliProject(Environment.CurrentDirectory, AppContext.BaseDirectory);

    /// <summary>Testable core of <see cref="FindRepoCliProject()"/>: searches the given roots upward.</summary>
    internal static string? FindRepoCliProject(params string[] startDirectories)
    {
        foreach (var start in startDirectories)
        {
            DirectoryInfo? dir;
            try
            {
                dir = new DirectoryInfo(start);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                continue;
            }

            for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "application", "src", "Ashlar.CLI", "Ashlar.CLI.csproj");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        return null;
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
