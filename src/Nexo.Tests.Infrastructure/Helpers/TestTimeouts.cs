namespace Nexo.Tests.Infrastructure.Helpers;

/// <summary>
/// Central timeout constants for tests. Use with [Fact(Timeout = TestTimeouts.Integration)] etc.
/// Aligns with docs/Testing.md and blame-hang-timeout (1.5x max per-test timeout).
///
/// Pick the constant that matches what the test TOUCHES, not how long it happens to take
/// today: a timeout sized to healthy duration turns an instrumented or loaded run into a
/// red build, which is a worse failure than the hang it was meant to catch.
/// </summary>
public static class TestTimeouts
{
    /// <summary>60 seconds for Integration tests.</summary>
    public const int Integration = 60_000;

    /// <summary>
    /// FileSystemWatcher integration: parallel net8+net10 test hosts must serialize; the waiter polls until the lock is free.
    /// </summary>
    public const int FileSystemPipelineIntegration = 240_000;

    /// <summary>
    /// 120 seconds for in-process stress bursts (e.g. the PeerExecutor 120-request burst).
    /// A hang net, not a budget: healthy runs finish in seconds, but the 60 s Integration
    /// bound tripped intermittently under kernel-coverage-gate instrumentation on a loaded runner.
    /// </summary>
    public const int Stress = 120_000;

    /// <summary>90 seconds for E2E tests.</summary>
    public const int E2E = 90_000;

    /// <summary>35 seconds for Dogfood Block 1 (observation pipeline).</summary>
    public const int Dogfood = 35_000;

    /// <summary>15 seconds for quick unit/integration tests.</summary>
    public const int Quick = 15_000;

    /// <summary>10 seconds for very fast tests (mesh capabilities, etc.).</summary>
    public const int Fast = 10_000;

    /// <summary>30 seconds for mesh/discover/advertise.</summary>
    public const int Mesh = 30_000;

    /// <summary>
    /// 8 minutes for host-touching (E2E / ProdStyle) tests. A HANG NET, not a
    /// performance budget.
    /// </summary>
    /// <remarks>
    /// Deliberately far above what these tests take when healthy — several run in
    /// single-digit seconds. The value has to clear the worst legitimate case, and the
    /// worst case is a coverlet-instrumented run on a loaded CI runner, where the same
    /// suite has gone from ~4.5 to ~11 minutes end to end. Sizing this to observed
    /// healthy duration converts an instrumented slow run into a red build, which is
    /// how a 60-second bound was chosen first and why it failed.
    ///
    /// 240s was the next guess and it too was breached by a legitimate run: under
    /// kernel-coverage-gate instrumentation a bridged suite case was killed at the
    /// 240s cap while still making progress, and in a sibling run a queued
    /// thread-pool work item executed 6m17s late (see "Intermittent timeouts under
    /// instrumentation" in docs/production-readiness/KernelCoverageGate-Findings.md).
    /// Eight minutes clears the worst stall observed so far with margin.
    ///
    /// What it must still catch is a test that never finishes at all — the failure mode
    /// that cost three CI runs of 30, 60 and 45 minutes and produced no diagnosis.
    /// </remarks>
    public const int HostTouching = 480_000;
}
