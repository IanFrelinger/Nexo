using Ashlar.Infrastructure.HostProcess;

namespace Ashlar.CLI.Commands;

/// <summary>Doctor remediation.</summary>
internal static class DoctorRemediation
{
    /// <summary>Creates a new RunAsync instance.</summary>
    public static async Task<DoctorRemediationReport> RunAsync(
        BootstrapAssessment assessment,
        bool osSupported,
        bool cliSmokePassed,
        string profile,
        bool includeOptional,
        bool autoApproveFixes,
        bool json,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var actions = BuildActionPlan(assessment, osSupported, cliSmokePassed, profile, includeOptional);

        if (dryRun)
        {
            var dryAttempts = actions
                .Select(action => new DoctorRemediationAttempt(
                    action.Id,
                    action.Problem,
                    action.Command,
                    Attempted: false,
                    Success: false,
                    Status: "dry-run",
                    Message: "Would run remediation command (dry run; no action taken).",
                    ExitCode: -1,
                    FollowUp: action.FollowUp))
                .OrderBy(a => a.Id, StringComparer.Ordinal)
                .ToList();
            return new DoctorRemediationReport
            {
                FixEnabled = true,
                AutoApproveFixes = autoApproveFixes,
                Attempts = dryAttempts,
            };
        }

        var attempts = new List<DoctorRemediationAttempt>();
        foreach (var action in actions)
        {
            if (!autoApproveFixes && json)
            {
                attempts.Add(new DoctorRemediationAttempt(
                    action.Id,
                    action.Problem,
                    action.Command,
                    Attempted: false,
                    Success: false,
                    Status: "confirmation-required",
                    Message: "JSON mode requires --yes to execute remediation commands.",
                    ExitCode: -1,
                    FollowUp: action.FollowUp));
                continue;
            }

            if (!autoApproveFixes && action.RequiresExplicitConfirmation && !json)
            {
                Console.Write($"{action.DisplayName} remediation requires confirmation. Run now? [y/N]: ");
                var answer = Console.ReadLine();
                if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                {
                    attempts.Add(new DoctorRemediationAttempt(
                        action.Id,
                        action.Problem,
                        action.Command,
                        Attempted: false,
                        Success: false,
                        Status: "skipped",
                        Message: "Skipped by operator. Re-run with --yes to auto-approve.",
                        ExitCode: -1,
                        FollowUp: action.FollowUp));
                    continue;
                }
            }

            var exitCode = await RunShellAsync(action.Command, cancellationToken).ConfigureAwait(false);
            var success = exitCode == 0;
            attempts.Add(new DoctorRemediationAttempt(
                action.Id,
                action.Problem,
                action.Command,
                Attempted: true,
                Success: success,
                Status: success ? "fixed" : "failed",
                Message: success
                    ? "Remediation command completed successfully."
                    : $"Remediation command exited with code {exitCode}.",
                ExitCode: exitCode,
                FollowUp: action.FollowUp));
        }

        return new DoctorRemediationReport
        {
            FixEnabled = true,
            AutoApproveFixes = autoApproveFixes,
            Attempts = attempts
                .OrderBy(attempt => attempt.Id, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static IReadOnlyList<DoctorRemediationAction> BuildActionPlan(
        BootstrapAssessment assessment,
        bool osSupported,
        bool cliSmokePassed,
        string profile,
        bool includeOptional)
    {
        var actions = new List<DoctorRemediationAction>();
        if (!osSupported)
        {
            actions.Add(new DoctorRemediationAction(
                "unsupported-os",
                "Unsupported host OS",
                "Host operating system is unsupported for the bootstrap automation.",
                "echo \"Unsupported host OS.\"" ,
                RequiresExplicitConfirmation: false,
                "Use container lane: docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help"));
            return actions;
        }

        if (assessment.MissingRequired.Any())
        {
            actions.Add(new DoctorRemediationAction(
                "bootstrap-apply",
                "Install missing required dependencies",
                "Required dependencies are missing from the current host profile.",
                $"dotnet run --project application/src/Ashlar.CLI -- bootstrap apply --profile {profile} {(includeOptional ? "--include-optional " : string.Empty)}--yes",
                RequiresExplicitConfirmation: true,
                "Re-run doctor after install to verify dependencies and smoke checks."));
        }

        if (!cliSmokePassed)
        {
            actions.Add(new DoctorRemediationAction(
                "cli-build",
                "Build CLI project",
                "CLI smoke check failed and often indicates stale build outputs or restore issues.",
                "dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj",
                RequiresExplicitConfirmation: true,
                "Re-run doctor to confirm cli smoke passes."));
        }

        return actions;
    }

    private static async Task<int> RunShellAsync(string command, CancellationToken cancellationToken)
    {
        var result = await TimedProcess.RunShellAsync(command, TimedProcess.RemediationTimeout, cancellationToken)
            .ConfigureAwait(false);
        return result.TimedOut ? TimedProcess.TimeoutExitCode : result.ExitCode;
    }

    private sealed record DoctorRemediationAction(
        string Id,
        string DisplayName,
        string Problem,
        string Command,
        bool RequiresExplicitConfirmation,
        string FollowUp);
}
