using System.Collections;
using System.Diagnostics;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The environment every child process the gate spawns receives: an explicit ALLOWLIST copied by
/// name from the certifier's own environment — never the environment itself.
/// </summary>
/// <remarks>
/// <para><b>Why an allowlist.</b> The certifier spawns two kinds of child on the author's behalf:
/// <c>dotnet msbuild</c> over the author's project (<see cref="EvaluatedBrickProject"/>), whose
/// targets run whatever the project says — an <c>&lt;Exec&gt;</c> included — and the witness
/// replay runner (<see cref="LocalProcessExecutionBackend"/>), which executes the candidate and
/// every mutant of it. Both used to inherit the certifier's full environment minus a few named
/// variables. The record signing keys live in environment variables
/// (<c>ASHLAR_CERT_DEV_HMAC_KEY</c>, <c>ASHLAR_CERT_ED25519_KEY</c>), so an author's build target,
/// or a candidate's <c>ExecuteAsync</c>, could read the key that was about to sign its own
/// certificate and mint one over any source text. A denylist cannot close that: it enumerates the
/// secrets its author knew about, and the operator's other credentials — CI tokens, cloud keys,
/// feed passwords — are not on it. So a child gets only what the SDK and the runtime demonstrably
/// need, by name, and anything under <see cref="DeniedPrefix"/> is refused even if a future edit
/// puts it on the list.</para>
///
/// <para><b>How the list was sized.</b> By making <c>dotnet msbuild -restore -t:Build</c> of a
/// brick project and the replay runner work under it, in the repository's dev container and on
/// Windows, and adding names as they proved necessary — not by passing <c>DOTNET_*</c> or
/// <c>NUGET_*</c> wholesale. Each group below says what it is for. Deliberately absent:
/// <c>DOTNET_STARTUP_HOOKS</c> (code injected into every process the runtime starts; the runner
/// executes exactly what the job names), <c>DOTNET_ROLL_FORWARD</c> (the runner's own
/// <c>runtimeconfig.json</c> decides which runtime replays the mutants, and a host-wide
/// <c>LatestMajor</c> rolled net8.0 children onto .NET 10 in this repository's own container),
/// the <c>DOTNET_MSBUILD_SDK_RESOLVER_*</c> / <c>MSBuildSDKsPath</c> / <c>MSBuildExtensionsPath</c>
/// family (they point a nested evaluation at the SDK that happens to be running the certifier),
/// and the MSBuild property names — <c>TargetFramework</c>, <c>Configuration</c>, <c>Platform</c> —
/// that MSBuild reads as global properties, which is how a <c>dotnet test</c> host used to inject
/// its own <c>TargetFramework</c> into the author's build.</para>
///
/// <para><b>What this does NOT close (Linux).</b> A child running as the same uid can still read
/// <c>/proc/&lt;certifier pid&gt;/environ</c>: the certifier's INITIAL environment block, which is
/// exactly where a key exported from the launching shell lives. The allowlist governs what a child
/// is given, not what it can read. Closing that needs one of: not holding keys in the environment
/// at all (a key file readable only by the certifier's uid, or a signer the certifier calls and
/// never sees the key of); running the children under a different uid or in their own PID
/// namespace; or <c>hidepid=2</c> on <c>/proc</c>. <c>ChildProcessEnvironmentTests</c> documents
/// the gap with a test that goes red when the platform closes it, so it is not mistaken for
/// closed.</para>
/// </remarks>
internal static class ChildProcessEnvironment
{
    /// <summary>
    /// No variable under this prefix reaches a child, whatever the allowlist says. Every certification
    /// secret (<c>ASHLAR_CERT_DEV_HMAC_KEY</c>, <c>ASHLAR_CERT_ED25519_KEY</c>) and every
    /// certification knob (<c>ASHLAR_CERT_NUGET_CONFIG</c>, which the loader passes on the command line
    /// instead) lives under it.
    /// </summary>
    internal const string DeniedPrefix = "ASHLAR_CERT_";

    /// <summary>The variables a child may inherit, by exact name (case-insensitive on Windows).</summary>
    internal static readonly IReadOnlyList<string> Allowlist =
    [
        // Where things are, for every child: the muxer on PATH, the user's home (the NuGet cache and
        // NuGet.Config live under it), temp, locale and time zone, native library lookup.
        "PATH", "HOME", "USER", "LOGNAME", "TMPDIR", "TMP", "TEMP", "TZ",
        "LANG", "LC_ALL", "LC_CTYPE", "LC_MESSAGES", "LD_LIBRARY_PATH",

        // Windows system locations the runtime, MSBuild's Exec (ComSpec) and NuGet (APPDATA,
        // LOCALAPPDATA, USERPROFILE) resolve through. Absent on other platforms.
        "SystemRoot", "SystemDrive", "windir", "ComSpec", "PATHEXT", "OS",
        "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432", "ProgramData", "ALLUSERSPROFILE",
        "APPDATA", "LOCALAPPDATA", "USERPROFILE", "USERNAME", "NUMBER_OF_PROCESSORS", "PROCESSOR_ARCHITECTURE",

        // XDG base directories: NuGet's HTTP and plugin caches on Linux when set.
        "XDG_DATA_HOME", "XDG_CONFIG_HOME", "XDG_CACHE_HOME",

        // .NET host location (an SDK task that launches an apphost needs DOTNET_ROOT), multilevel
        // lookup, the CLI's home when HOME is not writable, and the first-run / noise knobs, so a
        // certifier's quiet configuration stays quiet in its children.
        "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_ROOT_X86", "DOTNET_ROOT_ARM64", "DOTNET_MULTILEVEL_LOOKUP",
        "DOTNET_CLI_HOME", "DOTNET_CLI_UI_LANGUAGE", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "DOTNET_NOLOGO",
        "DOTNET_CLI_TELEMETRY_OPTOUT", "DOTNET_GENERATE_ASPNET_CERTIFICATE", "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH",
        "DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE",

        // Globalization mode: a host without ICU sets these, and a child without them fails at startup.
        "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY",

        // NuGet cache and fallback-folder locations, for the restore. Credentials are not among them.
        "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "NUGET_PLUGINS_CACHE_PATH", "NUGET_SCRATCH",
        "NUGET_FALLBACK_PACKAGES", "NUGET_XMLDOC_MODE",

        // A restore behind a proxy. Both spellings, because Unix environment names are case-sensitive.
        "HTTP_PROXY", "HTTPS_PROXY", "NO_PROXY", "http_proxy", "https_proxy", "no_proxy",

        // Node reuse off (CI sets it) so the author's build leaves no MSBuild node behind.
        "MSBUILDDISABLENODEREUSE",
    ];

    private static readonly StringComparer NameComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static readonly HashSet<string> AllowedNames = new(Allowlist, NameComparer);

    /// <summary>Whether a variable of this name may cross into a child.</summary>
    internal static bool IsAllowed(string name) =>
        !name.StartsWith(DeniedPrefix, StringComparison.OrdinalIgnoreCase) && AllowedNames.Contains(name);

    /// <summary>
    /// Replaces <paramref name="startInfo"/>'s inherited environment with the allowlisted subset of
    /// this process's. Call it BEFORE setting the variables the child must have (<c>DOTNET_GCHeapHardLimit</c>,
    /// <c>MSBUILDTERMINALLOGGER</c>…): it clears first.
    /// </summary>
    internal static void Apply(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        startInfo.Environment.Clear();
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string name && entry.Value is string value && IsAllowed(name))
                startInfo.Environment[name] = value;
        }
    }
}
