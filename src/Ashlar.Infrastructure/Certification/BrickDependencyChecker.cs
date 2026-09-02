namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Validates brick project dependencies against the certification allow-list, against the
/// references MSBuild actually gives the project rather than the ones its <c>.csproj</c> spells
/// out.
/// </summary>
/// <remarks>
/// This class used to <c>XDocument.Load</c> the <c>.csproj</c> and walk its descendants. That reads
/// ONE file out of the chain MSBuild evaluates, and a <c>Directory.Build.props</c> sitting beside
/// the project is part of that chain: a <c>ProjectReference</c> and a <c>Newtonsoft.Json</c>
/// <c>PackageReference</c> declared there were ADMITTED and signed, with both DLLs sitting in the
/// built output, while the identical items written into the csproj were correctly refused. Where an
/// item was declared is not a fact about the brick; what the brick depends on is. So the question
/// is put to MSBuild — see <see cref="EvaluatedBrickProject"/> — and answered for the whole import
/// chain at once.
/// </remarks>
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

        EvaluatedBrickProject project;
        List<EvaluatedItem> declaredAnalyzers;
        List<EvaluatedItem> declaredReferences;
        try
        {
            project = EvaluatedBrickProject.Evaluate(projectPath);
            // Inside the same guard as the evaluation: telling an author-declared item from an
            // SDK-declared one needs the SDK root, and a project where that cannot be established
            // is a project whose references the gate cannot judge. That is a refusal to report as a
            // violation, not an exception to escape a method whose contract is a result.
            declaredAnalyzers = project.Analyzers.Where(a => !project.IsSdkDeclared(a)).ToList();
            declaredReferences = project.References.Where(r => !project.IsSdkDeclared(r)).ToList();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            // A project whose references cannot be established is not a project with no references.
            // Fail closed: the refusal carries MSBuild's own reason.
            violations.Add(ex.Message);
            return new DependencyCheckResult(false, violations);
        }

        foreach (var projectRef in project.ProjectReferences)
        {
            violations.Add(
                $"ProjectReference forbidden: {projectRef.Identity}"
                + DeclaredIn(projectPath, projectRef));
        }

        // An <Analyzer> is an assembly Roslyn loads INTO the compilation, and a source generator is
        // an analyzer: it writes code straight into the assembly without ever appearing as a
        // Compile item, so no amount of care about the compile set can see it. The allow-list is
        // about what a brick may contain, and generated code is contained in the brick. The SDK's
        // own analyzers are implicit in every project and are not the candidate's doing.
        foreach (var analyzer in declaredAnalyzers)
        {
            violations.Add(
                $"Analyzer forbidden: {analyzer.Identity} — an analyzer assembly may be a SOURCE GENERATOR, whose "
                + "output is compiled into the brick without ever being a source file the content hash, the "
                + "analyzer fence or the mutation leg can see. Fix: remove the <Analyzer> item; reference "
                + $"{string.Join(" / ", BuildTimeOnlyPackages)} as a build-time-only PackageReference if you want "
                + "the Ashlar analyzers to run."
                + DeclaredIn(projectPath, analyzer));
        }

        // A raw <Reference Include="X"><HintPath>…</HintPath></Reference> is a dependency that
        // never passes through the PackageReference allow-list at all: the DLL is copied to the
        // brick's output, the brick binds to its types and certifies, and the packed brick declares
        // no such dependency. The XML scan looked only at PackageReference and ProjectReference, so
        // this walked straight past the two-package rule.
        foreach (var reference in declaredReferences)
        {
            violations.Add(
                $"Reference forbidden: {reference.Identity} — a raw assembly reference bypasses the package "
                + "allow-list entirely while still putting its DLL in the brick's output. Fix: express the "
                + "dependency as a PackageReference (only Ashlar.Brick.Contracts + Ashlar.Authoring are allowed), "
                + "or drop it."
                + DeclaredIn(projectPath, reference));
        }

        foreach (var packageRef in project.PackageReferences)
        {
            var include = packageRef.Identity;
            if (string.IsNullOrWhiteSpace(include))
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
                    + "process. A certified brick has at most two runtime dependencies."
                    + DeclaredIn(projectPath, packageRef));
                continue;
            }

            violations.Add($"PackageReference '{include}' is not allowed (only Ashlar.Brick.Contracts + Ashlar.Authoring, "
                + $"plus {string.Join(" / ", BuildTimeOnlyPackages)} referenced build-time-only with ExcludeAssets=\"runtime;compile\")"
                + DeclaredIn(projectPath, packageRef));
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
    private static bool IsBuildTimeOnly(EvaluatedItem packageRef)
    {
        // MSBuild unifies the attribute and element forms into one metadata value, so the two-shape
        // lookup this used to need is gone with the XML.
        var excludeAssets = packageRef.Meta("ExcludeAssets");
        if (HasToken(excludeAssets, "all"))
            return true;

        return HasToken(excludeAssets, "runtime") && HasToken(excludeAssets, "compile");
    }

    /// <summary>
    /// " (declared in Directory.Build.props)", when the item came from somewhere other than the
    /// project file itself. Without it the author reads a refusal naming a reference their csproj
    /// does not contain, and has nowhere to look.
    /// </summary>
    private static string DeclaredIn(string projectPath, EvaluatedItem item)
    {
        var origin = item.Meta("DefiningProjectFullPath");
        if (string.IsNullOrWhiteSpace(origin))
            return string.Empty;

        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(origin), Path.GetFullPath(projectPath), comparison)
            ? string.Empty
            : $" (declared in {Path.GetFileName(origin)}, which MSBuild imports into this project)";
    }

    private static bool HasToken(string? value, string token) =>
        !string.IsNullOrWhiteSpace(value)
        && value!.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                 .Any(p => string.Equals(p.Trim(), token, StringComparison.OrdinalIgnoreCase));
}
