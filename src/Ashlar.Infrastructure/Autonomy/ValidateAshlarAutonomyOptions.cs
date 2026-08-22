using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Autonomy;

namespace Ashlar.Infrastructure.Autonomy;

/// <summary>
/// Fail-closed validation for <see cref="AshlarAutonomyOptions"/>, wired with
/// <c>ValidateOnStart</c> so a misconfigured host refuses to boot rather than running a
/// half-configured autonomy loop.
///
/// <para><b>Deliberately no air-gapped refusal.</b> The MCP/A2A protocol surfaces refuse
/// enablement under <c>ASHLAR_DEPLOYMENT_PROFILE=airgapped</c> because they are network
/// ingress. Autonomy is the opposite case: the spec REQUIRES the loop's controls to work
/// fully offline (R6.3 — pause, rollback, quarantine, and demotion may not depend on
/// egress), and an air-gapped host running a local model is a legitimate, arguably ideal,
/// deployment. Refusing it here would enforce the reverse of the invariant.</para>
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class ValidateAshlarAutonomyOptions : IValidateOptions<AshlarAutonomyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AshlarAutonomyOptions options)
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
                $"{nameof(AshlarAutonomyOptions.SessionImage)} is required when "
                + $"{nameof(AshlarAutonomyOptions.UseSandboxSessions)}=true: an unnamed image cannot be "
                + "attested, and an unattestable environment must never reach a certificate.");
        }

        if (!string.IsNullOrWhiteSpace(options.SessionImageDigest)
            && !options.SessionImageDigest.Trim().StartsWith("sha256:", StringComparison.Ordinal))
        {
            failures.Add(
                $"{nameof(AshlarAutonomyOptions.SessionImageDigest)} must be an image identity of the form "
                + "'sha256:<hex>' (the value attestation records and certificates carry as image-digest), "
                + $"not '{options.SessionImageDigest}': a pin that can never match would refuse every "
                + "session, and a tag is not an identity.");
        }

        if (options.BuildCandidateInSession && !options.UseSandboxSessions)
        {
            failures.Add(
                $"{nameof(AshlarAutonomyOptions.BuildCandidateInSession)}=true requires "
                + $"{nameof(AshlarAutonomyOptions.UseSandboxSessions)}=true: the in-session build leg "
                + "cannot run without sessions, and every iteration would refuse fail-closed — an "
                + "always-refusing loop is a configuration error, not a policy.");
        }

        if (options.ExecuteCandidateInSession && !options.BuildCandidateInSession)
        {
            failures.Add(
                $"{nameof(AshlarAutonomyOptions.ExecuteCandidateInSession)}=true requires "
                + $"{nameof(AshlarAutonomyOptions.BuildCandidateInSession)}=true: the execution leg "
                + "loads the session-built candidate assembly, so there is nothing to execute "
                + "without the build leg.");
        }

        if (options.RetentionWindow < 1)
        {
            failures.Add(
                $"{nameof(AshlarAutonomyOptions.RetentionWindow)} must be at least 1: autonomy requires a "
                + "reachable rollback target (I-3 — every autonomous action is reversible or it does not run).");
        }

        if (options.IterationCeilingSeconds < 1)
            failures.Add($"{nameof(AshlarAutonomyOptions.IterationCeilingSeconds)} must be positive.");

        if (options.ThroughputGuardFactor <= 1)
            failures.Add($"{nameof(AshlarAutonomyOptions.ThroughputGuardFactor)} must be greater than 1.");

        if (options.LineageDemotionThreshold < 1)
            failures.Add($"{nameof(AshlarAutonomyOptions.LineageDemotionThreshold)} must be at least 1.");

        if (options.WatchMinInvocations < 1)
            failures.Add($"{nameof(AshlarAutonomyOptions.WatchMinInvocations)} must be at least 1.");

        if (options.WatchMaxErrorRateDelta < 0)
            failures.Add($"{nameof(AshlarAutonomyOptions.WatchMaxErrorRateDelta)} cannot be negative.");

        if (options.WatchMaxLatencyFactor <= 0)
            failures.Add($"{nameof(AshlarAutonomyOptions.WatchMaxLatencyFactor)} must be positive.");

        if (options.CadenceFloorSeconds < 0)
            failures.Add($"{nameof(AshlarAutonomyOptions.CadenceFloorSeconds)} cannot be negative.");

        if (options.WatchMaxInvocationSeconds < 0)
            failures.Add($"{nameof(AshlarAutonomyOptions.WatchMaxInvocationSeconds)} cannot be negative (0 disables the leg).");

        if (options.DigestIntervalSeconds < 0)
            failures.Add($"{nameof(AshlarAutonomyOptions.DigestIntervalSeconds)} cannot be negative (0 disables the job).");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
