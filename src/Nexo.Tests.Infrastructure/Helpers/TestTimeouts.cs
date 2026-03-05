namespace Nexo.Tests.Infrastructure.Helpers;

/// <summary>
/// Central timeout constants for tests. Use with [Fact(Timeout = TestTimeouts.Integration)] etc.
/// Aligns with docs/Testing.md and blame-hang-timeout (1.5x max per-test timeout).
/// </summary>
public static class TestTimeouts
{
    /// <summary>60 seconds for Integration tests.</summary>
    public const int Integration = 60_000;

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
}
