namespace Ashlar.Tests.Infrastructure.Tests.Mesh;

/// <summary>
/// Environment switches for optional Docker mesh lab tests (see <see cref="MeshLabDockerE2ETests"/>).
/// </summary>
internal static class MeshLabDockerEnv
{
    /// <summary>When true, <see cref="MeshLabDockerFixture"/> starts compose and tests run verify scripts.</summary>
    public static bool IsEnabled => IsTruthy("ASHLAR_RUN_MESH_LAB");

    /// <summary>When true, the deep verify script runs (on by default when mesh lab is enabled).</summary>
    public static bool RunDeep => IsEnabled && !IsFalsy("ASHLAR_MESH_LAB_SKIP_DEEP");

    public static bool IsTruthy(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFalsy(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }
}
