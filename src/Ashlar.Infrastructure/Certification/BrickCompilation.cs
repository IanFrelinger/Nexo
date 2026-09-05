using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using BrickBuildCompileOptions = Ashlar.Core.Application.Certification.Models.BrickCompileOptions;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The one place a <see cref="BrickBuildCompileOptions"/> becomes Roslyn options, so every in-process
/// compile of a candidate — the analyzer fence, the mutation catalog's binding compilation, every
/// mutant — parses and compiles the SAME program the build did.
/// </summary>
/// <remarks>
/// <para>Before this existed each of those sites called <c>CSharpSyntaxTree.ParseText(source)</c> and
/// <c>new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)</c>: no preprocessor symbols,
/// unchecked arithmetic, the host compiler's language version, no global usings. The build used the
/// evaluated project. Same bytes, same signed <c>contentHash</c>, different program — and the fence
/// and the mutation leg judged the one that did not ship.</para>
///
/// <para><c>null</c> options mean "no build to match" (a candidate compiled only in-process, whose
/// in-process compile therefore IS the program) and reproduce the previous defaults exactly.</para>
///
/// <para>The <c>global using</c> directives the build compiled are supplied as a sibling syntax tree,
/// which is how csc saw them too (<c>obj/.../GlobalUsings.g.cs</c> is its own compilation unit). One
/// directive is filtered: a project alias <c>DomainBrick</c> for <c>Ashlar.Core.Domain.Bricks.Brick</c>
/// duplicates the alias <see cref="CandidateSourceWrapper"/> injects into every certification compile,
/// and a duplicate alias is a compile error (CS1537) rather than a harmless repeat. Dropping it
/// changes nothing about how names bind, because it names the same type. A <c>DomainBrick</c> alias
/// for anything ELSE is refused by <see cref="AssertHonourable"/>: dropping that one would change
/// the program, and keeping it cannot compile.</para>
/// </remarks>
internal static class BrickCompilation
{
    /// <summary>The alias every certification compile injects, and the type it names.</summary>
    private const string WrapperAlias = "DomainBrick";
    private const string WrapperAliasTarget = "Ashlar.Core.Domain.Bricks.Brick";

    /// <summary>Parse options for the candidate and for every tree compiled beside it.</summary>
    public static CSharpParseOptions ParseOptions(BrickBuildCompileOptions? options)
    {
        if (options is null)
        {
            return CSharpParseOptions.Default;
        }

        if (!LanguageVersionFacts.TryParse(options.LanguageVersion, out var version))
        {
            throw new InvalidOperationException(Refusal(
                $"it was compiled as C# '{options.LanguageVersion}', a language version the gate's own compiler "
                + $"(Microsoft.CodeAnalysis {typeof(CSharpCompilation).Assembly.GetName().Version}) does not know, "
                + "so the fence and the mutation leg cannot parse the program the build compiled. Fix: give the brick "
                + "a <LangVersion> this gate supports, or certify with a gate built against a newer compiler"));
        }

        return CSharpParseOptions.Default
            .WithLanguageVersion(version)
            .WithPreprocessorSymbols(options.PreprocessorSymbols);
    }

    /// <summary>Compilation options for the candidate: overflow checking, nullable context, unsafe.</summary>
    public static CSharpCompilationOptions CompilationOptions(BrickBuildCompileOptions? options, OutputKind outputKind)
    {
        var result = new CSharpCompilationOptions(outputKind);
        if (options is null)
        {
            return result;
        }

        if (!Enum.TryParse<NullableContextOptions>(options.Nullable, ignoreCase: true, out var nullable))
        {
            throw new InvalidOperationException(Refusal(
                $"its nullable context is recorded as '{options.Nullable}', which is not one the gate's compiler "
                + "knows (Disable, Enable, Warnings, Annotations)"));
        }

        return result
            .WithOverflowChecks(options.CheckOverflow)
            .WithNullableContextOptions(nullable)
            .WithAllowUnsafe(options.AllowUnsafe);
    }

    /// <summary>
    /// The build's <c>global using</c> directives as one sibling tree, or none. Parsed with the same
    /// options as the candidate, so a directive that is only legal under the build's language version
    /// is legal here too.
    /// </summary>
    public static IReadOnlyList<SyntaxTree> CompanionTrees(
        BrickBuildCompileOptions? options, CancellationToken cancellationToken = default)
    {
        if (options is null || options.GlobalUsings.Count == 0)
        {
            return Array.Empty<SyntaxTree>();
        }

        var kept = options.GlobalUsings.Where(directive => !DuplicatesWrapperAlias(directive)).ToList();
        if (kept.Count == 0)
        {
            return Array.Empty<SyntaxTree>();
        }

        var text = string.Join("\n", kept) + "\n";
        return new[]
        {
            CSharpSyntaxTree.ParseText(
                text, ParseOptions(options), path: "GlobalUsings.certified.g.cs", cancellationToken: cancellationToken)
        };
    }

    /// <summary>
    /// Refuses options the in-process legs cannot honour, so the refusal lands in the loader — before
    /// any leg runs and before anything is executed — rather than surfacing later as "the analyzer gate
    /// crashed". Every check here is one the legs would otherwise hit mid-run.
    /// </summary>
    public static void AssertHonourable(BrickBuildCompileOptions options)
    {
        var parse = ParseOptions(options);
        CompilationOptions(options, OutputKind.DynamicallyLinkedLibrary);

        foreach (var directive in options.GlobalUsings)
        {
            var unit = CSharpSyntaxTree.ParseText(directive + "\n", parse).GetCompilationUnitRoot();
            var parsed = unit.Usings.Count == 1 && unit.Members.Count == 0 && unit.Externs.Count == 0
                && unit.AttributeLists.Count == 0
                ? unit.Usings[0]
                : null;
            if (parsed is null || !parsed.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                || parsed.ContainsDiagnostics)
            {
                throw new InvalidOperationException(Refusal(
                    $"the build compiled a global using the gate cannot read as one directive: '{directive}'"));
            }

            if (parsed.Alias is { } alias
                && alias.Name.Identifier.ValueText == WrapperAlias
                && !DuplicatesWrapperAlias(directive))
            {
                throw new InvalidOperationException(Refusal(
                    $"the build compiled '{directive}', a global using alias named '{WrapperAlias}' for something other "
                    + $"than {WrapperAliasTarget}. The certification harness injects its own '{WrapperAlias}' alias for "
                    + $"{WrapperAliasTarget} into every in-process compile, so the two cannot coexist (CS1537) and dropping "
                    + "the project's would change how the brick's names bind. Fix: rename the project's alias, or drop "
                    + $"it and refer to {WrapperAliasTarget} directly"));
            }
        }

        var companions = CompanionTrees(options);
        var errors = companions
            .SelectMany(tree => tree.GetDiagnostics())
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Take(3)
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(Refusal(
                "the build's global using directives do not parse under the recorded language version: "
                + string.Join(" | ", errors.Select(e => e.ToString()))));
        }
    }

    /// <summary>
    /// Compiles one wrapped candidate exactly as the build would have: same parse options, same
    /// compilation options, same global usings beside it.
    /// </summary>
    public static Task<CompilationResult> CompileAsync(
        RoslynCodeAnalysisService compiler,
        string wrappedSource,
        string assemblyName,
        string outputPath,
        IEnumerable<string>? references,
        BrickBuildCompileOptions? options,
        CancellationToken cancellationToken)
        => compiler.CompileAsync(
            wrappedSource,
            assemblyName,
            outputPath,
            references,
            ParseOptions(options),
            CompilationOptions(options, OutputKind.DynamicallyLinkedLibrary),
            CompanionTrees(options, cancellationToken),
            cancellationToken);

    /// <summary>
    /// True when <paramref name="directive"/> is a <c>global using DomainBrick = ...Brick;</c> naming the
    /// exact type the wrapper's alias names, in any spelling of the qualifier.
    /// </summary>
    private static bool DuplicatesWrapperAlias(string directive)
    {
        var unit = CSharpSyntaxTree.ParseText(directive + "\n").GetCompilationUnitRoot();
        if (unit.Usings.Count != 1 || unit.Usings[0].Alias is not { } alias)
        {
            return false;
        }

        if (alias.Name.Identifier.ValueText != WrapperAlias)
        {
            return false;
        }

        var target = unit.Usings[0].Name?.ToString().Replace(" ", string.Empty) ?? string.Empty;
        if (target.StartsWith("global::", StringComparison.Ordinal))
        {
            target = target["global::".Length..];
        }

        return target == WrapperAliasTarget;
    }

    private static string Refusal(string because) =>
        "Brick project refused: the gate cannot compile the program the build compiled — " + because
        + ". Certification signs a verdict about the compiled program, so a brick whose compile options the "
        + "in-process legs cannot honour is refused rather than judged under different ones.";
}
