using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Analyzers;

/// <summary>
/// Enforces one <see cref="BrickConstraintManifest"/> semantically over a candidate compilation
/// (extension spec Part A §2): the analyzer is constructed WITH the manifest instance, so the
/// rules it enforces and the instruction text it restates are, by construction, the same object
/// the proposer's prompt was rendered from (A2.3 — one source, used twice). Violation messages
/// end with the manifest instruction verbatim (A3.2).
///
/// Enforcement is symbol-based, not textual (A2.2): a fully-qualified call or an alias resolves
/// to the same symbol and is caught, while a token inside a comment or string is not.
///
/// Honesty discipline — what this analyzer checks, exactly:
/// <list type="bullet">
/// <item><see cref="BrickConstraintManifest.AllowedUsings"/>: every using directive's body text
/// must be on the allowlist (exact-text semantics, same normalization as the generation-side
/// validator). Empty allowlist = unrestricted.</item>
/// <item><see cref="BrickConstraintManifest.ForbiddenApiTokens"/>: object creations,
/// invocations and member references whose resolved symbol matches a token
/// (<c>Type.Member</c> bans that member; a bare <c>Type</c> bans creations and every member
/// of that type).</item>
/// <item><see cref="BrickConstraintManifest.ForbiddenNamespaces"/>: object creations,
/// invocations, member references and using directives whose resolved symbol lives inside a
/// forbidden namespace or any sub-namespace of one.</item>
/// </list>
/// A type that is merely declared but never created or dereferenced (for example a
/// <c>Random?</c> local that stays null) is not flagged; structural rules
/// (namespace/class-name/required declarations) stay with the generation-side validator.
///
/// NOT attribute-discovered: this type deliberately has no <c>[DiagnosticAnalyzer]</c>
/// attribute because it cannot be constructed without a manifest. The compiler's analyzer
/// discovery never instantiates it; only <c>AnalyzerFenceGate</c> attaches it, per candidate,
/// with that candidate's manifest.
/// </summary>
#pragma warning disable RS1001 // Missing [DiagnosticAnalyzer]: intentional, see doc comment above.
public sealed class BrickConstraintManifestAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1001
{
    /// <summary>Diagnostic id for a using directive outside the manifest allowlist.</summary>
    public const string DisallowedUsingId = "NEXO0010";

    /// <summary>Diagnostic id for a reference matching a forbidden API token.</summary>
    public const string ForbiddenApiId = "NEXO0011";

    /// <summary>Diagnostic id for a reference into a forbidden namespace.</summary>
    public const string ForbiddenNamespaceId = "NEXO0012";

    /// <summary>
    /// The manifest-derived diagnostic ids. Unlike the brick-scoped catalog rules these apply to
    /// every line of the compilation, so the certification gate uses this set to exempt text the
    /// candidate's proposer did not write (the wrapper-injected preamble).
    /// </summary>
    public static readonly ImmutableArray<string> ManifestRuleIds =
        ImmutableArray.Create(DisallowedUsingId, ForbiddenApiId, ForbiddenNamespaceId);

    private const string Category = "Nexo.TrustLoop";

    private static readonly DiagnosticDescriptor DisallowedUsing = new(
        DisallowedUsingId,
        title: "Using directive is outside the constraint manifest allowlist",
        messageFormat: "Using directive 'using {0};' is not on the manifest allowlist — remove it (constraint: {1})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The brick's constraint manifest allowlists the using directives a candidate may declare.");

    private static readonly DiagnosticDescriptor ForbiddenApi = new(
        ForbiddenApiId,
        title: "Reference matches a forbidden API token in the constraint manifest",
        messageFormat: "'{0}' matches forbidden API token '{1}' — remove it (constraint: {2})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The brick's constraint manifest denylists specific APIs; resolved-symbol matching means qualification and aliasing cannot dodge the rule.");

    private static readonly DiagnosticDescriptor ForbiddenNamespace = new(
        ForbiddenNamespaceId,
        title: "Reference resolves into a forbidden namespace in the constraint manifest",
        messageFormat: "'{0}' resolves into forbidden namespace '{1}' — remove it (constraint: {2})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The brick's constraint manifest denylists whole namespaces; any resolved symbol inside one (or a sub-namespace) is a violation.");

    private readonly BrickConstraintManifest _manifest;
    private readonly ImmutableArray<ApiToken> _tokens;

    /// <summary>Creates the analyzer for one manifest instance (spec A2.3: same object, second use).</summary>
    public BrickConstraintManifestAnalyzer(BrickConstraintManifest manifest)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        _tokens = manifest.ForbiddenApiTokens.Select(ApiToken.Parse).ToImmutableArray();
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DisallowedUsing, ForbiddenApi, ForbiddenNamespace);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        if (_manifest.AllowedUsings.Count > 0 || _manifest.ForbiddenNamespaces.Count > 0)
            context.RegisterSyntaxNodeAction(OnUsingDirective, SyntaxKind.UsingDirective);

        if (_tokens.Length > 0 || _manifest.ForbiddenNamespaces.Count > 0)
        {
            context.RegisterOperationAction(
                OnOperation,
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.PropertyReference,
                OperationKind.FieldReference,
                OperationKind.EventReference,
                OperationKind.MethodReference);
        }
    }

    private void OnUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var directive = (UsingDirectiveSyntax)context.Node;
        if (directive.Name is null)
            return;

        if (_manifest.AllowedUsings.Count > 0)
        {
            // Same normalization as the generation-side validator: the text between the
            // `using` keyword and the `;`, trimmed, compared ordinally.
            var start = directive.UsingKeyword.Span.End;
            var end = directive.SemicolonToken.Span.Start;
            var body = directive.SyntaxTree.GetText().ToString(
                Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end)).Trim();

            if (!_manifest.AllowedUsings.Contains(body, StringComparer.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DisallowedUsing, directive.GetLocation(), body, _manifest.UsingsInstruction()));
            }
        }

        if (_manifest.ForbiddenNamespaces.Count > 0)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(directive.Name, context.CancellationToken).Symbol;
            var nsDisplay = symbol switch
            {
                INamespaceSymbol ns => ns.ToDisplayString(),
                INamedTypeSymbol type => NamespaceOf(type),
                _ => null,
            };
            var forbidden = nsDisplay is null ? null : MatchForbiddenNamespace(nsDisplay);
            if (forbidden is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForbiddenNamespace,
                    directive.GetLocation(),
                    directive.Name.ToString(),
                    forbidden,
                    _manifest.ForbiddenNamespaceInstruction(forbidden)));
            }
        }
    }

    private void OnOperation(OperationAnalysisContext context)
    {
        var (member, containingType) = context.Operation switch
        {
            IObjectCreationOperation creation => ((ISymbol?)creation.Constructor, creation.Constructor?.ContainingType),
            IInvocationOperation invocation => (invocation.TargetMethod, invocation.TargetMethod.ContainingType),
            IMemberReferenceOperation reference => (reference.Member, reference.Member.ContainingType),
            _ => (null, null),
        };
        if (member is null || containingType is null)
            return;

        var display = member is IMethodSymbol { MethodKind: MethodKind.Constructor }
            ? $"new {containingType.ToDisplayString()}()"
            : $"{containingType.ToDisplayString()}.{member.Name}";
        var location = context.Operation.Syntax.GetLocation();

        foreach (var token in _tokens)
        {
            if (token.Matches(member, containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForbiddenApi, location, display, token.Raw, _manifest.ForbiddenApiInstruction(token.Raw)));
                break; // One API finding per operation is enough signal to repair it.
            }
        }

        var forbiddenNs = MatchForbiddenNamespace(NamespaceOf(containingType));
        if (forbiddenNs is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ForbiddenNamespace, location, display, forbiddenNs, _manifest.ForbiddenNamespaceInstruction(forbiddenNs)));
        }
    }

    private string? MatchForbiddenNamespace(string namespaceDisplay)
    {
        foreach (var forbidden in _manifest.ForbiddenNamespaces)
        {
            if (namespaceDisplay.Equals(forbidden, StringComparison.Ordinal)
                || namespaceDisplay.StartsWith(forbidden + ".", StringComparison.Ordinal))
            {
                return forbidden;
            }
        }

        return null;
    }

    private static string NamespaceOf(INamedTypeSymbol type) =>
        type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : "";

    /// <summary>
    /// One parsed forbidden-API token. <c>Type.Member</c> bans that member of that type;
    /// a bare <c>Type</c> bans creations of and every member of that type. Type matching
    /// accepts the simple name (<c>DateTime</c>) or a fully-qualified suffix
    /// (<c>System.DateTime</c>), always against the RESOLVED containing type.
    /// </summary>
    private readonly struct ApiToken
    {
        public string Raw { get; }
        private readonly string? _typePart;
        private readonly string _memberOrType;

        private ApiToken(string raw, string? typePart, string memberOrType)
        {
            Raw = raw;
            _typePart = typePart;
            _memberOrType = memberOrType;
        }

        public static ApiToken Parse(string raw)
        {
            var trimmed = raw.Trim();
            var lastDot = trimmed.LastIndexOf('.');
            return lastDot > 0 && lastDot < trimmed.Length - 1
                ? new ApiToken(raw, trimmed.Substring(0, lastDot), trimmed.Substring(lastDot + 1))
                : new ApiToken(raw, null, trimmed);
        }

        public bool Matches(ISymbol member, INamedTypeSymbol containingType)
        {
            if (_typePart is null)
                return TypeMatches(containingType, _memberOrType);

            return member.Name.Equals(_memberOrType, StringComparison.Ordinal)
                && TypeMatches(containingType, _typePart);
        }

        private static bool TypeMatches(INamedTypeSymbol type, string namePattern)
        {
            if (type.Name.Equals(namePattern, StringComparison.Ordinal))
                return true;

            var display = type.ToDisplayString();
            return display.Equals(namePattern, StringComparison.Ordinal)
                || display.EndsWith("." + namePattern, StringComparison.Ordinal);
        }
    }
}
