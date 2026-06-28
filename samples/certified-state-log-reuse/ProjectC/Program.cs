using Nexo.Certification.State;

namespace CertifiedStateLogReuse.ProjectC;

internal static class Program
{
    public static async Task<int> Main(string[] args) => await ProgramEntry.RunAsync(args);
}

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        var artifactRoot = args.ElementAtOrDefault(0)
            ?? throw new InvalidOperationException(
                "Usage: ProjectC <path-to-Nexo.Certified.PhaseWitness> [hmac-key] [--bind-live]");

        var hmacKey = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal) && arg != artifactRoot)
            ?? "attested-state-log-test-hmac";
        var bindLive = args.Contains("--bind-live", StringComparer.Ordinal);
        var trust = bindLive
            ? AttestedStateLogArtifactVerifier.VerifyBundledArtifacts(artifactRoot, hmacKey, bindLiveStateSidecar: true)
            : AttestedStateLogArtifactVerifier.VerifyBundledArtifacts(artifactRoot, hmacKey);

        if (!trust.Trusted)
        {
            await Console.Error.WriteLineAsync($"UNTRUSTED: {trust.FailureCode} — {trust.Reason}").ConfigureAwait(false);
            return 2;
        }

        await Console.Out.WriteLineAsync($"TRUSTED verifiedTransitions={trust.VerifiedTransitions}").ConfigureAwait(false);
        return 0;
    }
}
