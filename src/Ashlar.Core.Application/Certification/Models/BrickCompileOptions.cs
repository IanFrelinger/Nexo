using Ashlar.Certification.Contracts;

namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// The compile options a brick's build actually used — the half of "which program is this" that the
/// source text does not carry.
/// </summary>
/// <remarks>
/// <para>A C# source file is not a program until the compiler is told how to read it: which
/// preprocessor symbols are defined (so which <c>#if</c> branches exist), which language version
/// (so how names and overloads bind), whether arithmetic is checked, the nullable context, whether
/// unsafe code is allowed, and which <c>global using</c> directives the project injected. Two
/// projects with a byte-identical <c>Brick.cs</c> — and therefore an identical signed
/// <c>contentHash</c> — compile DIFFERENT programs when any of these differ. The SDK defines
/// <c>NET</c>, <c>NET8_0</c> and <c>NETCOREAPP</c> for every net8.0 project on its own, so the split
/// is reachable from a completely stock <c>.csproj</c>.</para>
///
/// <para>Every in-process leg of the gate (the analyzer fence, the mutation catalog, the per-mutant
/// compiles) consumes ONE instance of this, and the loader derives it from the compiler's own record
/// of the build (the compilation-options block csc writes into the portable PDB, plus the
/// <c>global using</c> file it compiled), never from a hard-coded list. <c>null</c> on a request
/// means "no build to match": the in-process compile IS the program, and the legs use their
/// defaults exactly as before.</para>
///
/// <para>The record carries these as a signed <c>compile-options</c> input
/// (<see cref="ToCertificationInput"/>) rather than inside <c>contentHash</c>: the content hash is
/// the identity every downstream verifier recomputes from the source text alone
/// (<c>CertificationTrustVerifier</c>, the hot-swap host, revocation), and folding options into it
/// would silently break every one of them. Under the signature, a sibling input is equally
/// tamper-evident, and a reader can see which program was judged.</para>
/// </remarks>
public sealed record BrickCompileOptions
{
    /// <summary>The <see cref="CertificationInput.Kind"/> under which the options are recorded.</summary>
    public const string InputKind = "compile-options";

    /// <summary>
    /// The effective C# language version, as csc records it (<c>"12.0"</c>). Roslyn's
    /// <c>"default"</c> means the certifying compiler's own default.
    /// </summary>
    public string LanguageVersion { get; init; } = "default";

    /// <summary>Every preprocessor symbol the compilation was given, SDK-implicit ones included.</summary>
    public IReadOnlyList<string> PreprocessorSymbols { get; init; } = Array.Empty<string>();

    /// <summary>Whether integer arithmetic is checked by default (<c>CheckForOverflowUnderflow</c>).</summary>
    public bool CheckOverflow { get; init; }

    /// <summary>The nullable context, by Roslyn's name: <c>Disable</c>, <c>Enable</c>, <c>Warnings</c> or <c>Annotations</c>.</summary>
    public string Nullable { get; init; } = "Disable";

    /// <summary>Whether unsafe blocks are allowed.</summary>
    public bool AllowUnsafe { get; init; }

    /// <summary>
    /// Every <c>global using</c> directive the build compiled from SDK-generated source, as directive
    /// text (<c>global using global::System;</c>, <c>global using Clock = global::System.DateTime;</c>).
    /// These change how names in the brick bind without appearing in the brick's own file.
    /// </summary>
    public IReadOnlyList<string> GlobalUsings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// One line that says exactly which program these options describe, stable under reordering of
    /// the symbol and using lists.
    /// </summary>
    public string Canonical()
    {
        var symbols = string.Join(",", PreprocessorSymbols.OrderBy(s => s, StringComparer.Ordinal));
        var usings = string.Join(" ", GlobalUsings.OrderBy(u => u, StringComparer.Ordinal));
        return "langVersion=" + LanguageVersion
            + ";checkOverflow=" + (CheckOverflow ? "true" : "false")
            + ";nullable=" + Nullable
            + ";unsafe=" + (AllowUnsafe ? "true" : "false")
            + ";symbols=" + symbols
            + ";globalUsings=" + usings;
    }

    /// <summary>
    /// The options as a certificate input: the canonical line is the id, so a reader sees it, and its
    /// digest is the hash, so the signature binds it.
    /// </summary>
    public CertificationInput ToCertificationInput()
    {
        var canonical = Canonical();
        return new CertificationInput
        {
            Kind = InputKind,
            Id = canonical,
            Hash = BrickContentHasher.ComputeSha256(canonical),
        };
    }
}
