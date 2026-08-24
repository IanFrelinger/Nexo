using Ashlar.Manifest;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>
/// A one-glance answer to "is THIS project ready to ship?", aggregating the signals the product
/// loop already exposes — does it verify, is there an operator key, is the signed ledger intact,
/// has it been certified — plus the single next action that moves it forward. The environment
/// doctor answers "is my machine set up"; this answers "is my project set up", and the two are
/// deliberately separate questions with separate verdicts.
/// </summary>
public sealed record DoctorProjectReadiness
{
    /// <summary>Verify passed every course.</summary>
    public required bool Verified { get; init; }

    /// <summary><c>present</c>, <c>absent</c>, or <c>corrupt</c>.</summary>
    public required string KeyStatus { get; init; }

    /// <summary>Operator key fingerprint when present; otherwise null.</summary>
    public string? Fingerprint { get; init; }

    /// <summary><c>intact</c>, <c>none</c>, or <c>corrupt</c>.</summary>
    public required string LedgerStatus { get; init; }

    /// <summary>How many signed entries the ledger holds.</summary>
    public required int LedgerEntries { get; init; }

    /// <summary>A short verdict word for the block header.</summary>
    public required string Verdict { get; init; }

    /// <summary>The single next action that advances readiness.</summary>
    public required string NextStep { get; init; }

    /// <summary>True only when verified, keyed, and the ledger is intact with at least one entry.</summary>
    public bool Ready => Verified && KeyStatus == "present" && LedgerStatus == "intact";

    /// <summary>
    /// Assesses the project at <paramref name="directory"/>, or returns null when it is not an
    /// ashlar project (no manifest/policy) — the doctor then simply omits the project block. The
    /// key directory is overridable for tests; production passes null to use the machine key.
    /// </summary>
    public static DoctorProjectReadiness? Assess(string directory, string? keyDir = null)
    {
        var manifestPath = Path.Combine(directory, "ashlar.yaml");
        var policyPath = Path.Combine(directory, "ashlar.policy.yaml");
        if (!File.Exists(manifestPath) || !File.Exists(policyPath))
        {
            return null;
        }

        var verification = ProjectVerifier.Verify(
            File.ReadAllText(manifestPath), File.ReadAllText(policyPath), directory);
        var verified = verification.Verified;

        string keyStatus;
        string? fingerprint = null;
        try
        {
            var signer = OperatorKey.TryLoad(keyDir);
            keyStatus = signer is null ? "absent" : "present";
            fingerprint = signer?.Fingerprint;
        }
        catch (InvalidOperationException)
        {
            keyStatus = "corrupt";
        }

        string ledgerStatus;
        var ledgerEntries = 0;
        try
        {
            var chain = new InstanceLedger(Path.Combine(directory, ".ashlar")).VerifyChain();
            ledgerEntries = chain.Count;
            ledgerStatus = chain.Count == 0 ? "none" : "intact";
        }
        catch (InvalidOperationException)
        {
            ledgerStatus = "corrupt";
        }

        var (verdict, next) = Decide(verified, keyStatus, ledgerStatus);
        return new DoctorProjectReadiness
        {
            Verified = verified,
            KeyStatus = keyStatus,
            Fingerprint = fingerprint,
            LedgerStatus = ledgerStatus,
            LedgerEntries = ledgerEntries,
            Verdict = verdict,
            NextStep = next,
        };
    }

    // The verdict ladder, most-broken first: a corrupt ledger or key is a hard block; then
    // failing verification; then the ordinary "verified but not yet certified" progression.
    private static (string Verdict, string NextStep) Decide(bool verified, string keyStatus, string ledgerStatus)
    {
        if (ledgerStatus == "corrupt")
        {
            return ("BLOCKED", "the signed ledger is corrupt — history was altered; inspect .ashlar/ledger/");
        }
        if (keyStatus == "corrupt")
        {
            return ("BLOCKED", "the operator key is corrupt — run `ashlar keys init --rotate` to write a fresh pair");
        }
        if (!verified)
        {
            return ("NOT READY", "run `ashlar verify` to see the failing course, then fix it");
        }
        if (keyStatus == "absent")
        {
            return ("NOT CERTIFIED", "run `ashlar keys init`, then `ashlar verify` to certify this project");
        }
        if (ledgerStatus == "none")
        {
            return ("READY TO CERTIFY", "run `ashlar verify` to write the first signed ledger entry");
        }
        return ("CERTIFIED", "certified and ready — nothing to do");
    }
}
