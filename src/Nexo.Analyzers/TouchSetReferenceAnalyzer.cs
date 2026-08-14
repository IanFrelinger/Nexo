using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Nexo.Analyzers;

/// <summary>
/// Enforces an objective's declared touch-set against the candidate's ACTUAL resolved
/// reference graph (autonomous self-extension spec R3.2, analyzer leg): the classifier
/// tiered the objective from its declaration; this analyzer proves the candidate stayed
/// inside it. An undeclared reference into the trust kernel is the kernel-touch-smuggling
/// case and gets its own diagnostic.
///
/// Constructed programmatically by the certification gate with the SAME declaration that
/// classified the tier and derived the sandbox mounts — one declaration, three
/// enforcement surfaces. Namespaces-as-primitives (not the TouchSet type itself) because
/// this assembly is referenced BY Core.Application in the analyzer role; a library
/// reference back would be a cycle.
///
/// Honesty discipline — what this analyzer checks, exactly: resolved symbol references
/// (creations, invocations, member accesses, base types/interfaces, using directives).
/// A reflection string (<c>Type.GetType("Nexo.Policies…")</c>), a transitive dependency
/// whose IMPLEMENTATION touches the kernel, or generated-at-runtime code produce no
/// resolvable candidate-graph reference and are deliberately not claimed here — they are
/// the swap host's and runtime watch's legs of the R3.2 triple check.
///
/// Baseline namespaces (<c>System*</c>, <c>Microsoft.CSharp*</c>, and the
/// <c>Nexo.Core.Domain*</c> brick contracts every brick must reference) are always
/// permitted: a touch-set declares what an extension may AFFECT beyond the floor every
/// brick stands on, and flagging <c>Task</c> or <c>BrickOutput</c> would be noise, not
/// enforcement.
/// </summary>
#pragma warning disable RS1001 // Not attribute-discovered: constructed programmatically with a touch-set.
public sealed class TouchSetReferenceAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1001
{
    /// <summary>Diagnostic id for a resolved reference outside the declared touch-set.</summary>
    public const string OutsideTouchSetId = "NEXO0013";

    /// <summary>Diagnostic id for an undeclared resolved reference into the trust kernel.</summary>
    public const string UndeclaredKernelReferenceId = "NEXO0014";

    /// <summary>The touch-set rule ids (whole-compilation rules; wrapper text is gate-exempt).</summary>
    public static readonly ImmutableArray<string> TouchSetRuleIds =
        ImmutableArray.Create(OutsideTouchSetId, UndeclaredKernelReferenceId);

    private const string Category = "Nexo.TrustLoop";

    private static readonly string[] BaselineNamespacePrefixes =
    {
        "System",
        "Microsoft.CSharp",
        "Nexo.Core.Domain",
    };

    private static readonly DiagnosticDescriptor OutsideTouchSet = new(
        OutsideTouchSetId,
        title: "Reference outside the objective's declared touch-set",
        messageFormat: "'{0}' resolves into namespace '{1}', which the objective's touch-set does not declare — the candidate reaches beyond its classified blast radius",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The tier was classified from the declared touch-set; a candidate referencing beyond it invalidates the classification.");

    private static readonly DiagnosticDescriptor UndeclaredKernelReference = new(
        UndeclaredKernelReferenceId,
        title: "Undeclared reference into the trust kernel",
        messageFormat: "'{0}' resolves into trust-kernel namespace '{1}' without a declaration — kernel-touch smuggling is a certification FAIL (I-1)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No artifact produced by the loop may modify the trust kernel without human admission; an undeclared kernel reference forfeits certification outright.");

    private readonly ImmutableArray<string> _declaredNamespaces;
    private readonly ImmutableArray<string> _kernelNamespacePrefixes;

    /// <summary>
    /// Creates the analyzer for one objective's declaration. <paramref name="declaredNamespaces"/>
    /// are exact-or-prefix permitted namespaces beyond the baseline;
    /// <paramref name="kernelNamespacePrefixes"/> is the trust-kernel enumeration.
    /// </summary>
    public TouchSetReferenceAnalyzer(
        IEnumerable<string> declaredNamespaces,
        IEnumerable<string> kernelNamespacePrefixes)
    {
        _declaredNamespaces = (declaredNamespaces ?? throw new ArgumentNullException(nameof(declaredNamespaces)))
            .Select(n => n.Trim()).Where(n => n.Length > 0).ToImmutableArray();
        _kernelNamespacePrefixes = (kernelNamespacePrefixes ?? throw new ArgumentNullException(nameof(kernelNamespacePrefixes)))
            .Select(n => n.Trim()).Where(n => n.Length > 0).ToImmutableArray();
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(OutsideTouchSet, UndeclaredKernelReference);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(OnUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSymbolAction(OnNamedType, SymbolKind.NamedType);
        context.RegisterOperationAction(
            OnOperation,
            OperationKind.ObjectCreation,
            OperationKind.Invocation,
            OperationKind.PropertyReference,
            OperationKind.FieldReference,
            OperationKind.EventReference,
            OperationKind.MethodReference);
    }

    private void OnUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var directive = (UsingDirectiveSyntax)context.Node;
        if (directive.Name is null)
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(directive.Name, context.CancellationToken).Symbol;
        var ns = symbol switch
        {
            INamespaceSymbol n => n.ToDisplayString(),
            INamedTypeSymbol t => NamespaceOf(t),
            _ => null,
        };
        if (ns is not null)
            Check(context.ReportDiagnostic, directive.GetLocation(), directive.Name.ToString(), ns);
    }

    private void OnNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var location = type.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return;

        if (type.BaseType is { } baseType)
            Check(context.ReportDiagnostic, location, baseType.ToDisplayString(), NamespaceOf(baseType));
        foreach (var iface in type.Interfaces)
            Check(context.ReportDiagnostic, location, iface.ToDisplayString(), NamespaceOf(iface));
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
        Check(context.ReportDiagnostic, context.Operation.Syntax.GetLocation(), display, NamespaceOf(containingType));
    }

    private void Check(Action<Diagnostic> report, Location location, string display, string ns)
    {
        if (ns.Length == 0 || MatchesAny(ns, BaselineNamespacePrefixes))
            return;
        if (MatchesAny(ns, _declaredNamespaces))
            return;

        report(MatchesAny(ns, _kernelNamespacePrefixes)
            ? Diagnostic.Create(UndeclaredKernelReference, location, display, ns)
            : Diagnostic.Create(OutsideTouchSet, location, display, ns));
    }

    private static bool MatchesAny(string ns, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (ns.Equals(prefix, StringComparison.Ordinal)
                || ns.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NamespaceOf(INamedTypeSymbol type) =>
        type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : "";
}
