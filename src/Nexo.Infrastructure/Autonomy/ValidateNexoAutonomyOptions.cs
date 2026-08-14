using Microsoft.Extensions.Options;

namespace Nexo.Infrastructure.Autonomy;

/// <summary>
/// Fail-closed validation for <see cref="NexoAutonomyOptions"/>, wired with
/// <c>ValidateOnStart</c> so a misconfigured host refuses to boot rather than running a
/// half-configured autonomy loop.
///
/// <para><b>Deliberately no air-gapped refusal.</b> The MCP/A2A protocol surfaces refuse
/// enablement under <c>NEXO_DEPLOYMENT_PROFILE=airgapped</c> because they are network
/// ingress. Autonomy is the opposite case: the spec REQUIRES the loop's controls to work
/// fully offline (R6.3 — pause, rollback, quarantine, and demotion may not depend on
/// egress), and an air-gapped host running a local model is a legitimate, arguably ideal,
/// deployment. Refusing it here would enforce the reverse of the invariant.</para>
/// </summary>
public sealed class ValidateNexoAutonomyOptions : IValidateOptions<NexoAutonomyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, NexoAutonomyOptions options)
    {
        if (!options.Enabled)
        {
            // Disabled is always valid: an off loop cannot be misconfigured into acting.
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.UseSandboxSessions && string.IsNullOrWhiteSpace(options.SessionImage))
        {
            failures.Add(
                $"{nameof(NexoAutonomyOptions.SessionImage)} is required when "
                + $"{nameof(NexoAutonomyOptions.UseSandboxSessions)}=true: an unnamed image cannot be "
                + "attested, and an unattestable environment must never reach a certificate.");
        }

        if (options.BuildCandidateInSession && !options.UseSandboxSessions)
        {
            failures.Add(
                $"{nameof(NexoAutonomyOptions.BuildCandidateInSession)}=true requires "
                + $"{nameof(NexoAutonomyOptions.UseSandboxSessions)}=true: the in-session build leg "
                + "cannot run without sessions, and every iteration would refuse fail-closed — an "
                + "always-refusing loop is a configuration error, not a policy.");
        }

        if (options.RetentionWindow < 1)
        {
            failures.Add(
                $"{nameof(NexoAutonomyOptions.RetentionWindow)} must be at least 1: autonomy requires a "
                + "reachable rollback target (I-3 — every autonomous action is reversible or it does not run).");
        }

        if (options.IterationCeilingSeconds < 1)
            failures.Add($"{nameof(NexoAutonomyOptions.IterationCeilingSeconds)} must be positive.");

        if (options.ThroughputGuardFactor <= 1)
            failures.Add($"{nameof(NexoAutonomyOptions.ThroughputGuardFactor)} must be greater than 1.");

        if (options.LineageDemotionThreshold < 1)
            failures.Add($"{nameof(NexoAutonomyOptions.LineageDemotionThreshold)} must be at least 1.");

        if (options.WatchMinInvocations < 1)
            failures.Add($"{nameof(NexoAutonomyOptions.WatchMinInvocations)} must be at least 1.");

        if (options.WatchMaxErrorRateDelta < 0)
            failures.Add($"{nameof(NexoAutonomyOptions.WatchMaxErrorRateDelta)} cannot be negative.");

        if (options.WatchMaxLatencyFactor <= 0)
            failures.Add($"{nameof(NexoAutonomyOptions.WatchMaxLatencyFactor)} must be positive.");

        if (options.CadenceFloorSeconds < 0)
            failures.Add($"{nameof(NexoAutonomyOptions.CadenceFloorSeconds)} cannot be negative.");

        if (options.WatchMaxInvocationSeconds < 0)
            failures.Add($"{nameof(NexoAutonomyOptions.WatchMaxInvocationSeconds)} cannot be negative (0 disables the leg).");

        if (options.DigestIntervalSeconds < 0)
            failures.Add($"{nameof(NexoAutonomyOptions.DigestIntervalSeconds)} cannot be negative (0 disables the job).");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
