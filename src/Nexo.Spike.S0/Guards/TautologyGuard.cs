using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0.Guards;

/// <summary>
/// Rejects tests that pass against the trivial stub (tautology bait).
/// </summary>
public static class TautologyGuard
{
    public static bool IsTautological(RedFailureReason reason) =>
        reason == RedFailureReason.PassedAgainstStub;
}
