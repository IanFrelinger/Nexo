using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// The write surface the harness's in-session legs need — what a session spec declares as
/// <see cref="SandboxSpec.ScratchPaths"/> so the Docker backend can seal everything else
/// read-only and still let the candidate build and execute. One place, so the paths the
/// legs write to and the paths the spec declares cannot drift apart.
///
/// <para>Why each entry: the candidate work directory and the execution root are the
/// legs' own constants; <c>/tmp</c> is where MSBuild and the compiler server keep their
/// pipes and temp files; <c>/root</c> is the toolchain's home in the SDK images the legs
/// target (first-run sentinel, telemetry store, NuGet's user config and caches) — the SDK
/// runs as root there and writes under <c>$HOME</c> even for an offline, package-free
/// restore. Everything else the toolchain touches lives on the image and is read.</para>
/// </summary>
public static class SessionScratchPaths
{
    /// <summary>Temp directory the toolchain's pipes and scratch files land in.</summary>
    public const string Temp = "/tmp";

    /// <summary>The toolchain's home in the SDK images the session legs target.</summary>
    public const string ToolchainHome = "/root";

    /// <summary>
    /// The full write surface for a session that builds and executes candidates:
    /// candidate work directory, execution root, temp, toolchain home.
    /// </summary>
    public static readonly IReadOnlyList<string> Default = new[]
    {
        SessionCandidateBuild.WorkDir,
        SessionExecutionBackend.ExecDir,
        Temp,
        ToolchainHome,
    };
}
