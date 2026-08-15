using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Nexo.Analyzers;

/// <summary>
/// Flags service registrations whose factory resolves the service's own type from the
/// provider: <c>services.AddSingleton&lt;IFoo&gt;(sp =&gt; sp.GetRequiredService&lt;IFoo&gt;())</c>
/// and factories that pass <c>sp.GetRequiredService&lt;IFoo&gt;()</c> into the implementation
/// they construct.
///
/// This is the DI-hang class: the registration passes <c>ValidateOnBuild</c> (the graph is
/// well-formed) and then recurses at resolution time — observed in this repo as a host that
/// hangs at startup with no exception while one thread repeats the factory hundreds of times.
/// A lazy indirection (<c>Lazy&lt;T&gt;</c>) or graph restructuring is the fix.
///
/// Honesty discipline: only generic registrations with a literal lambda factory are judged —
/// the service type must be an explicit type argument and the resolution must be a
/// <c>GetService&lt;T&gt;</c>/<c>GetRequiredService&lt;T&gt;</c> call with that same type
/// argument, syntactically inside the lambda. typeof-based registrations, method-group
/// factories, and resolutions reached through helper methods are left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfRecursiveRegistrationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic id for self-recursive service registration.</summary>
    public const string SelfRecursiveRegistrationId = "NEXO0005";

    private const string Category = "Nexo.TrustLoop";

    private static readonly DiagnosticDescriptor SelfRecursiveRegistration = new(
        SelfRecursiveRegistrationId,
        title: "Service factory resolves its own service type",
        messageFormat:
            "Registration of '{0}' uses a factory that resolves '{0}' from the service provider. "
            + "The graph passes ValidateOnBuild and then recurses at resolution time — the host hangs "
            + "or stack-overflows with no exception. Resolve the dependency lazily (Lazy<{0}>) or "
            + "restructure the registration.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A service factory that resolves its own service type re-enters itself at resolution "
            + "time. Build-time validation cannot catch it; the failure is a silent hang.");

    private const string ServiceCollectionMetadataName =
        "Microsoft.Extensions.DependencyInjection.IServiceCollection";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(SelfRecursiveRegistration);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Anchor on IServiceCollection: compilations that cannot see it cannot register
        // services, so the rule has nothing to say.
        var serviceCollection = context.Compilation.GetTypeByMetadataName(ServiceCollectionMetadataName);
        if (serviceCollection is null)
            return;

        context.RegisterOperationAction(
            operation => OnInvocation(operation, serviceCollection),
            OperationKind.Invocation);
    }

    private static void OnInvocation(OperationAnalysisContext context, INamedTypeSymbol serviceCollection)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Registration shape: a generic Add*/TryAdd* extension whose receiver is an
        // IServiceCollection. The service type is the first type argument.
        if (!method.Name.StartsWith("Add", StringComparison.Ordinal) &&
            !method.Name.StartsWith("TryAdd", StringComparison.Ordinal))
        {
            return;
        }

        if (method.TypeArguments.Length == 0)
            return;

        var extendedType = (method.ReducedFrom ?? method).Parameters.FirstOrDefault()?.Type;
        var receiverType = invocation.Instance?.Type ?? extendedType;
        if (receiverType is null || !ImplementsOrIs(receiverType, serviceCollection))
            return;

        var serviceType = method.TypeArguments[0];

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Value is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda })
                continue;

            foreach (var descendant in lambda.Body.DescendantsAndSelf())
            {
                if (descendant is IInvocationOperation resolve &&
                    resolve.TargetMethod is { Name: "GetRequiredService" or "GetService", TypeArguments.Length: 1 } &&
                    SymbolEqualityComparer.Default.Equals(resolve.TargetMethod.TypeArguments[0], serviceType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SelfRecursiveRegistration,
                        resolve.Syntax.GetLocation(),
                        serviceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    return;
                }
            }
        }
    }

    private static bool ImplementsOrIs(ITypeSymbol type, INamedTypeSymbol serviceCollection)
    {
        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, serviceCollection))
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, serviceCollection))
                return true;
        }

        return false;
    }
}
