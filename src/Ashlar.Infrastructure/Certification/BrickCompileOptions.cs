using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Closed-world compilation options the certifier uses for every gate-emitted assemble.
/// Authors cannot supply a language version, unsafe flag, or output kind; the blob is
/// hashed into the certificate so a later compiler bump is a new judge, not a silent one.
/// </summary>
public static class BrickCompileOptions
{
    /// <summary>Pinned language version. C# 13+ is the compiler-ceiling refusal (A7).</summary>
    public const LanguageVersion LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12;

    /// <summary>Human-readable ceiling name used in refusals and docs.</summary>
    public const string LanguageVersionName = "CSharp12";

    /// <summary>Canonical options blob recorded as the <c>compile-options</c> input.</summary>
    public static string CanonicalBlob { get; } =
        "language=CSharp12;output=Library;allowUnsafe=false;checkOverflow=false;optimization=Release;nullable=disable;concurrent=false";

    /// <summary>Parse options bound to the closed-world language version.</summary>
    public static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion);

    /// <summary>Emit options bound to the closed-world compilation surface.</summary>
    public static CSharpCompilationOptions CompilationOptions { get; } =
        new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: false,
            checkOverflow: false,
            optimizationLevel: OptimizationLevel.Release,
            concurrentBuild: false);
}
