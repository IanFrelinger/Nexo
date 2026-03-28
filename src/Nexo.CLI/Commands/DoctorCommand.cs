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

        AddOption(includeOptionalOpt);
        AddOption(profileOpt);
        AddOption(jsonOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            ctx.ExitCode = await ExecuteAsync(profile, includeOptional, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
    }

    internal static async Task<int> ExecuteAsync(
        string profile,
        bool includeOptional,
        bool json,
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
                    }
                },
                nextSteps = new
                {
                    nativeInstall = "bash scripts/install/install.sh --yes",
                    containerRun = "docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help"
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

            Console.WriteLine($"overall: {(overallOk ? "PASS" : "FAIL")}");
            Console.WriteLine("recommended next steps:");
            Console.WriteLine("  - native lane: bash scripts/install/install.sh --yes");
            Console.WriteLine("  - container lane: docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help");
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
