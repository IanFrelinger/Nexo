using System.Diagnostics.CodeAnalysis;
using System.Text;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>One in-session candidate build: what ran, where, and how it ended.</summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed record SessionBuildResult
{
    /// <summary>Whether the candidate built cleanly inside the session.</summary>
    public required bool Passed { get; init; }

    /// <summary>Exit code of the failing step, or 0 when <see cref="Passed"/>.</summary>
    public required int ExitCode { get; init; }

    /// <summary>The session toolchain's version string (<c>dotnet --version</c>), or "(none)" when unprobed.</summary>
    public required string ToolchainVersion { get; init; }

    /// <summary>Tail of the failing step's output — the diagnostic a human reads first.</summary>
    public required string OutputTail { get; init; }

    /// <summary>
    /// The compiler's diagnostics for the candidate, in the CANDIDATE's own line coordinates
    /// (the wrapper's preamble mapped away), first occurrence of each distinct message, capped.
    /// Empty when the failure was not a compile failure (restore, toolchain probe) or when the
    /// candidate built. This is what a proposer is handed to repair a build failure — it is
    /// about the candidate's own text and nothing else, so it discloses nothing of the witness.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Compiles the candidate INSIDE the iteration's attested sandbox session, over
/// <see cref="ISandboxedSession.ExecAsync"/> — the in-container toolchain leg that the
/// known-limitation docs tracked as "routing build/test through the session".
///
/// <para><b>What this proves, precisely.</b> The bytes compiled in-session are
/// <see cref="CandidateSourceWrapper.Wrap"/> of the candidate source — byte-identical to
/// what the analyzer leg and the mutation engine compile in the harness process (A1.2
/// extended: every certification-path compile, host or session, sees the same bytes). A
/// pass therefore attests: <i>this exact source compiles under the pinned session image's
/// toolchain, offline, against exactly the reference assemblies the harness supplied.</i>
/// The certificate's <c>session-build</c> input claims compilation containment only.
/// Execution containment is the SEPARATE <c>session-execution</c> claim, minted when the
/// harness's execution leg (<c>ExecuteCandidateInSession</c>) runs the witness, determinism,
/// and mutation legs over the assembly this build produced (<see cref="SessionExecutionBackend"/>);
/// with that leg off, those runs happen in the harness process and no input says otherwise.</para>
///
/// <para><b>How files travel.</b> Sessions may be sibling containers on a remote daemon
/// (the first-flight setup), where host bind mounts are meaningless — so nothing is
/// mounted. Source, reference assemblies, and project files stream through
/// <c>ExecAsync</c> as base64 argv chunks appended inside the session, then decoded.
/// Slower than a mount, but works against any daemon the session runner can reach and
/// keeps the session's write surface exactly its own filesystem.</para>
///
/// <para><b>Offline by construction.</b> The generated project has zero
/// <c>PackageReference</c>s (references are direct <c>HintPath</c> DLLs) and a
/// <c>NuGet.config</c> with cleared sources: restore succeeds from the SDK's installed
/// targeting packs alone, and anything that would need a download fails loudly instead of
/// waiting on a network the session does not have. The project targets
/// <c>net9.0</c>; hosts must pin a session image whose SDK can target it (e.g.
/// <c>mcr.microsoft.com/dotnet/sdk:9.0</c>) — an older SDK fails the build with NETSDK1045,
/// which is the correct fail-closed shape, not a silent host-side fallback.</para>
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class SessionCandidateBuild
{
    /// <summary>Certificate input kind for a passed in-session build.</summary>
    public const string InputKind = "session-build";

    /// <summary>Session-side working directory. Reset at the start of every build.</summary>
    public const string WorkDir = "/nexo-candidate";

    /// <summary>Where the built candidate assembly lands (with its copy-local references beside it).</summary>
    public const string BuiltAssemblyDirectory = WorkDir + "/bin/Release/net9.0";

    /// <summary>The built candidate assembly itself — the execution leg loads this.</summary>
    public const string BuiltAssemblyPath = BuiltAssemblyDirectory + "/Candidate.dll";

    private const int TailLength = 2000;

    /// <summary>
    /// Runs the build. Never throws for build-shaped failures — a failed step returns a
    /// <see cref="SessionBuildResult"/> with the step's diagnostic tail, so the harness can
    /// terminate the iteration as an explained failure with evidence attached.
    /// </summary>
    public static async Task<SessionBuildResult> RunAsync(
        ISandboxedSession session,
        ProposalCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(candidate);

        // 1. Toolchain probe: a session image without dotnet cannot run this leg, and the
        //    refusal should name that, not a cryptic build error.
        var probe = await session.ExecAsync(new[] { "dotnet", "--version" }, cancellationToken)
            .ConfigureAwait(false);
        if (!probe.Succeeded)
        {
            return Failed(probe,
                "the session image has no usable 'dotnet' toolchain (probe 'dotnet --version' failed)",
                toolchain: "(none)");
        }

        var toolchain = probe.StdOut.Trim();

        // 2. Clean room: empty our own constant path only, then recreate it. Emptied rather
        //    than removed because under a read-only rootfs the work directory IS a scratch
        //    mountpoint (SessionScratchPaths) and unlinking a mountpoint fails; and if the
        //    directory cannot be created at all the spec forgot to declare it — a loud
        //    "read-only file system" here is the right shape.
        var reset = await session.ExecAsync(
            new[] { "sh", "-c", $"mkdir -p {WorkDir} && find {WorkDir} -mindepth 1 -delete && mkdir -p {WorkDir}/refs" },
            cancellationToken)
            .ConfigureAwait(false);
        if (!reset.Succeeded)
            return Failed(reset, "resetting the session work directory failed", toolchain);

        // 3. The candidate, wrapped exactly as every certification-path compile wraps it.
        var wrapped = CandidateSourceWrapper.Wrap(candidate.SourceCode);
        var sourceUpload = await UploadFileAsync(
            session, $"{WorkDir}/Candidate.cs", Encoding.UTF8.GetBytes(wrapped), cancellationToken)
            .ConfigureAwait(false);
        if (sourceUpload is not null)
            return Failed(sourceUpload, "uploading the candidate source into the session failed", toolchain);

        // 4. Reference assemblies travel with the request — content, not host paths.
        var referenceItems = new StringBuilder();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in candidate.CompilationReferences)
        {
            if (string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
                continue;

            var fileName = SanitizeFileName(Path.GetFileName(reference));
            if (!seen.Add(fileName))
                continue;

            var upload = await UploadFileAsync(
                session, $"{WorkDir}/refs/{fileName}",
                await File.ReadAllBytesAsync(reference, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            if (upload is not null)
                return Failed(upload, $"uploading reference assembly '{fileName}' into the session failed", toolchain);

            referenceItems.Append("    <Reference Include=\"")
                .Append(Path.GetFileNameWithoutExtension(fileName))
                .Append("\"><HintPath>refs/")
                .Append(fileName)
                .Append("</HintPath></Reference>\n");
        }

        // 5. Offline guard: cleared package sources make any would-be download loud.
        var nugetUpload = await UploadFileAsync(
            session, $"{WorkDir}/NuGet.config", Encoding.UTF8.GetBytes(NuGetConfig), cancellationToken)
            .ConfigureAwait(false);
        if (nugetUpload is not null)
            return Failed(nugetUpload, "uploading NuGet.config into the session failed", toolchain);

        var project = ProjectFile(referenceItems.ToString());
        var projectUpload = await UploadFileAsync(
            session, $"{WorkDir}/Candidate.csproj", Encoding.UTF8.GetBytes(project), cancellationToken)
            .ConfigureAwait(false);
        if (projectUpload is not null)
            return Failed(projectUpload, "uploading the generated project file into the session failed", toolchain);

        // 6. The build itself — plain argv, no shell.
        var build = await session.ExecAsync(
            new[] { "dotnet", "build", $"{WorkDir}/Candidate.csproj", "-c", "Release", "--nologo", "-v", "quiet" },
            cancellationToken).ConfigureAwait(false);
        if (!build.Succeeded)
        {
            return Failed(build, "the candidate failed to compile inside the attested session", toolchain) with
            {
                Diagnostics = ExtractDiagnostics(build.StdOut + build.StdErr, candidate.SourceCode),
            };
        }

        return new SessionBuildResult
        {
            Passed = true,
            ExitCode = 0,
            ToolchainVersion = toolchain,
            OutputTail = Tail(build.StdOut + build.StdErr),
        };
    }

    /// <summary>
    /// The certificate input for a PASSED build: binds the toolchain identity and the exact
    /// candidate source hash into the signed record, keyed by session id — "this source
    /// compiled under this toolchain inside this attested session".
    /// </summary>
    public static CertificationInput ToInput(SessionBuildResult result, string sessionId, string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.Passed)
        {
            throw new InvalidOperationException(
                "A failed in-session build never becomes a certificate input; the iteration "
                + "terminates as an explained failure instead.");
        }

        return new CertificationInput
        {
            Kind = InputKind,
            Id = sessionId,
            Hash = BrickContentHasher.ComputeSha256(
                $"{result.ToolchainVersion}\n{BrickContentHasher.ComputeSha256(sourceCode)}"),
        };
    }

    private static Task<ProcessCommandResult?> UploadFileAsync(
        ISandboxedSession session, string path, byte[] bytes, CancellationToken cancellationToken) =>
        SessionFileTransfer.UploadAsync(session, path, bytes, cancellationToken);

    private static SessionBuildResult Failed(ProcessCommandResult step, string what, string toolchain) => new()
    {
        Passed = false,
        ExitCode = step.ExitCode,
        ToolchainVersion = toolchain,
        OutputTail = $"{what}: {Tail(step.StdErr + step.StdOut)}",
    };

    private static string Tail(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= TailLength ? trimmed : "…" + trimmed[^TailLength..];
    }

    /// <summary>Distinct diagnostics kept per build failure — the root causes, not the cascade.</summary>
    public const int MaxDiagnostics = 12;

    private static readonly System.Text.RegularExpressions.Regex DiagnosticLine = new(
        @"Candidate\.cs\((?<line>\d+),(?<col>\d+)\):\s*error\s+(?<code>[A-Z]+\d+):\s*(?<message>.*?)(?:\s*\[[^\]]*\])?\s*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

    /// <summary>
    /// The compiler's error diagnostics from a build transcript, mapped to the candidate's own
    /// line numbers (the certification wrapper prepends a preamble; see
    /// <see cref="CandidateSourceWrapper.MapToCandidateLine"/>), first occurrence of each
    /// distinct (code, message) kept in transcript order — a compile failure's first errors are
    /// its causes and the rest are the cascade — capped at <see cref="MaxDiagnostics"/>.
    /// </summary>
    public static IReadOnlyList<string> ExtractDiagnostics(string transcript, string candidateSource)
    {
        if (string.IsNullOrEmpty(transcript))
            return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in DiagnosticLine.Matches(transcript))
        {
            var code = m.Groups["code"].Value;
            var message = m.Groups["message"].Value.Trim();
            if (!seen.Add(code + "|" + message))
                continue;

            var wrappedLine = int.Parse(m.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var line = CandidateSourceWrapper.MapToCandidateLine(candidateSource, wrappedLine);
            var where = line > 0 ? $"line {line}, col {m.Groups["col"].Value}" : "outside your source";
            result.Add($"{where}: {code}: {message}");
            if (result.Count >= MaxDiagnostics)
                break;
        }

        return result;
    }

    private static string SanitizeFileName(string fileName)
    {
        var builder = new StringBuilder(fileName.Length);
        foreach (var c in fileName)
            builder.Append(char.IsLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_');
        return builder.ToString();
    }

    internal const string NuGetConfig = """
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
""";

    private static string ProjectFile(string referenceItems) => $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Candidate.cs" />
  </ItemGroup>
  <ItemGroup>
{referenceItems}  </ItemGroup>
</Project>
""";
}
