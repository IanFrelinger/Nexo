using Microsoft.CodeAnalysis.CSharp;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Compiler-ceiling tripwire: a requested language version above the closed-world
/// <see cref="BrickCompileOptions.LanguageVersion"/> is a named refusal, never a silent
/// upgrade to whatever SDK the host happens to have.
/// </summary>
public static class CompilerCeiling
{
    /// <summary>True when <paramref name="requested"/> is at or under the pinned ceiling.</summary>
    public static bool IsAtOrUnderCeiling(LanguageVersion requested) =>
        requested <= BrickCompileOptions.LanguageVersion;

    /// <summary>Refusal text used by the loader and the docs pin.</summary>
    public static string FormatRefusal(LanguageVersion requested) =>
        $"compiler-ceiling: requested language version {requested} exceeds the certifier ceiling "
        + $"{BrickCompileOptions.LanguageVersionName}. Lower the source to C# 12 or wait for a "
        + "documented ceiling bump that re-stamps certifier identity.";
}
