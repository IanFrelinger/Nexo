using System.Xml.Linq;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Validates brick project dependencies against the certification allow-list.</summary>
internal static class BrickDependencyChecker
{
    private static readonly HashSet<string> AllowedPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ashlar.Brick.Contracts",
        "Ashlar.Authoring"
    };

    /// <summary>
    /// Packages exempt from the two-package rule — but ONLY when referenced in a shape that
    /// genuinely keeps them out of the built brick, which means an <c>ExcludeAssets</c> covering
    /// <c>runtime</c> and <c>compile</c> (or <c>all</c>). See <see cref="IsBuildTimeOnly"/>.
    ///
    /// <para>Why this exists: the analyzer fence is one of the certification gate's five legs, and
    /// a brick author consuming Ashlar from nuget.org could not run it — the package shipped no
    /// analyzer assets, and adding the reference anyway made the brick UNCERTIFIABLE here, because
    /// this list allowed exactly two names. So the one leg an author could run locally was the one
    /// the rules refused.</para>
    ///
    /// <para>What the first version of this exemption got WRONG, and why the shape test is strict
    /// now: it accepted a bare <c>PrivateAssets="all"</c>. <c>PrivateAssets</c> stops assets flowing
    /// TRANSITIVELY to the referencing project's own consumers; it does nothing to the referencing
    /// project itself, which still gets the compile and runtime assets. And
    /// <c>Ashlar.Analyzers</c> deliberately ships a <c>lib/</c> leg beside
    /// <c>analyzers/dotnet/cs/</c> (the kernel consumes the assembly as an ordinary library), so
    /// with <c>PrivateAssets="all"</c> alone the analyzer DLL lands in the brick's output, the
    /// brick may reference analyzer types and still certify, the packed brick declares no such
    /// dependency, and a downstream consumer dies with <c>FileNotFoundException</c> at runtime.
    /// <c>ExcludeAssets="runtime;compile"</c> is the shape that keeps the analyzers RUNNING while
    /// putting nothing in the output — it is what the refusal names, and what the docs teach.</para>
    /// </summary>
    private static readonly HashSet<string> BuildTimeOnlyPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ashlar.Analyzers"
    };

    private static readonly string[] ForbiddenSourceTokens =
    [
        "Ashlar.Infrastructure",
        "Ashlar.Core.Application",
        "ProjectReference",
        "src/Ashlar",
        "/workspace"
    ];

    /// <summary>Check.</summary>
    public static DependencyCheckResult Check(string projectPath, string sourceCode)
    {
        var violations = new List<string>();

        if (!File.Exists(projectPath))
        {
            violations.Add($"Project file not found: {projectPath}");
            return new DependencyCheckResult(false, violations);
        }

        var projectDir = Path.GetDirectoryName(projectPath) ?? ".";
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        foreach (var projectRef in doc.Descendants(ns + "ProjectReference"))
        {
            violations.Add($"ProjectReference forbidden: {projectRef.Attribute("Include")?.Value}");
        }

        foreach (var packageRef in doc.Descendants(ns + "PackageReference"))
        {
            var include = packageRef.Attribute("Include")?.Value;
            if (include is null)
                continue;
            if (AllowedPackages.Contains(include))
                continue;

            if (BuildTimeOnlyPackages.Contains(include))
            {
                if (IsBuildTimeOnly(packageRef))
                    continue;

                // Refused, but with the exact edit — the reference is right, the SHAPE is not, and
                // a reference that reaches the runtime graph is a third dependency however it is
                // labelled.
                violations.Add(
                    $"PackageReference '{include}' is allowed only build-time-only, and this one is not. "
                    + $"Add ExcludeAssets=\"runtime;compile\" to it: <PackageReference Include=\"{include}\" Version=\"...\" ExcludeAssets=\"runtime;compile\" />. "
                    + "That shape keeps the analyzers RUNNING in your build — the analyzers asset group is not "
                    + "excluded — while putting nothing into the brick's output. PrivateAssets=\"all\" on its own is "
                    + "NOT enough, and is why this is refused: PrivateAssets only stops assets flowing on to YOUR "
                    + $"consumers, so '{include}' still contributes its compile and runtime assets to this project. "
                    + "The DLL lands in the brick's output, the brick can bind to its types and still certify, and "
                    + "the packed brick declares no such dependency — a FileNotFoundException in someone else's "
                    + "process. A certified brick has at most two runtime dependencies.");
                continue;
            }

            violations.Add($"PackageReference '{include}' is not allowed (only Ashlar.Brick.Contracts + Ashlar.Authoring, "
                + $"plus {string.Join(" / ", BuildTimeOnlyPackages)} referenced build-time-only with ExcludeAssets=\"runtime;compile\")");
        }

        foreach (var token in ForbiddenSourceTokens)
        {
            if (sourceCode.Contains(token, StringComparison.Ordinal))
                violations.Add($"Source contains forbidden token '{token}'");
        }

        return new DependencyCheckResult(violations.Count == 0, violations);
    }

    /// <summary>
    /// True when a PackageReference cannot contribute anything to the built brick:
    /// <c>ExcludeAssets</c> covers <c>all</c>, or covers both <c>runtime</c> and <c>compile</c>.
    /// Attribute OR element form — MSBuild accepts both, so a checker that only understood one
    /// would refuse a correctly-written project.
    /// </summary>
    /// <remarks>
    /// <para><c>PrivateAssets="all"</c> is deliberately NOT sufficient, and treating it as
    /// sufficient is the hole this method was rewritten to close. <c>PrivateAssets</c> governs
    /// TRANSITIVE flow only: it stops the package reaching projects that reference YOURS. The
    /// referencing project still receives every asset group, compile and runtime included. For a
    /// package that shipped only <c>analyzers/dotnet/cs/</c> the distinction would not matter;
    /// <c>Ashlar.Analyzers</c> also ships a <c>lib/</c> leg on purpose, so under
    /// <c>PrivateAssets="all"</c> its DLL is copied into the brick's output and is bindable from
    /// brick source. A brick could therefore call into the analyzer assembly, certify clean, pack
    /// without declaring the dependency, and fail at runtime in a consumer's process — an
    /// exemption that failed open exactly where the gate is supposed to be closed.</para>
    ///
    /// <para><c>ExcludeAssets="runtime;compile"</c> keeps the analyzers running (the
    /// <c>analyzers</c> asset group is untouched) and keeps the assembly out of the output and off
    /// the compile-time reference set. That is the shape the refusal names and the docs teach.</para>
    /// </remarks>
    private static bool IsBuildTimeOnly(XElement packageRef)
    {
        var excludeAssets = MetadataValue(packageRef, "ExcludeAssets");
        if (HasToken(excludeAssets, "all"))
            return true;

        return HasToken(excludeAssets, "runtime") && HasToken(excludeAssets, "compile");
    }

    private static string? MetadataValue(XElement packageRef, string name)
    {
        var attribute = packageRef.Attribute(name)?.Value;
        if (!string.IsNullOrWhiteSpace(attribute))
            return attribute;
        var ns = packageRef.Name.Namespace;
        return packageRef.Element(ns + name)?.Value;
    }

    private static bool HasToken(string? value, string token) =>
        !string.IsNullOrWhiteSpace(value)
        && value!.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                 .Any(p => string.Equals(p.Trim(), token, StringComparison.OrdinalIgnoreCase));
}
