using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Nexo.Analyzers;

/// <summary>
/// Flags brick code that reads an input key or writes an output key which the
/// brick's own <c>BrickInterface</c> never declares.
///
/// This is the <c>preferScaffold</c>/<c>qtShims</c> drift class: the declared
/// contract and the code that implements it are two separate strings that
/// nothing forced to agree. A caller reading the published interface sends
/// <c>preferScaffold</c>; the brick reads <c>preferScaffolding</c>; the input
/// silently falls through to its default and the feature quietly does nothing.
/// No exception, no failing test — the bag is stringly-typed and tolerant.
///
/// The analyzer only reasons about statically-known keys, so it cannot be
/// fooled into false positives by a computed key: a key it cannot resolve to a
/// compile-time constant is left alone. A type that declares no
/// <c>BrickInterface</c> at all is skipped entirely rather than guessed at.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrickInterfaceDriftAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic id for reading an undeclared input key.</summary>
    public const string UndeclaredInputId = "NEXO0001";

    /// <summary>Diagnostic id for writing an undeclared output key.</summary>
    public const string UndeclaredOutputId = "NEXO0002";

    private const string Category = "Nexo.BrickContract";

    private static readonly DiagnosticDescriptor UndeclaredInput = new(
        UndeclaredInputId,
        title: "Brick reads an input key it does not declare",
        messageFormat:
            "Brick '{0}' reads input key '{1}', which is not declared in its BrickInterface.Inputs. "
            + "Declared: {2}. A caller following the published interface will never populate this key, "
            + "so the read silently returns the default.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Every key a brick reads must appear in its declared BrickInterface.Inputs, otherwise the "
            + "published contract and the implementation disagree and the mismatch fails silently.");

    private static readonly DiagnosticDescriptor UndeclaredOutput = new(
        UndeclaredOutputId,
        title: "Brick writes an output key it does not declare",
        messageFormat:
            "Brick '{0}' writes output key '{1}', which is not declared in its BrickInterface.Outputs. "
            + "Declared: {2}. Consumers reading the published interface will not know this output exists.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Every key a brick emits must appear in its declared BrickInterface.Outputs, otherwise the "
            + "output is invisible to anything that binds against the published contract.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(UndeclaredInput, UndeclaredOutput);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var known = KnownTypes.From(context.Compilation);
        if (known is null)
            return;

        var compilation = context.Compilation;
        context.RegisterSymbolStartAction(
            symbolStart => OnBrickTypeStart(symbolStart, known, compilation),
            SymbolKind.NamedType);
    }

    private static void OnBrickTypeStart(
        SymbolStartAnalysisContext context,
        KnownTypes known,
        Compilation compilation)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.IsAbstract || !InheritsFrom(type, known.Brick))
            return;

        var contract = BrickContract.Collect(type, known, compilation, context.CancellationToken);

        // No BrickInterface anywhere in this type's own source: the contract lives
        // somewhere this analyzer cannot see (a base type outside the compilation,
        // a factory, configuration). Guessing here would only produce noise.
        if (!contract.Declared)
            return;

        context.RegisterOperationAction(
            operation => OnInvocation(operation, known, contract, type),
            OperationKind.Invocation);
    }

    private static void OnInvocation(
        OperationAnalysisContext context,
        KnownTypes known,
        BrickContract contract,
        INamedTypeSymbol brick)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (method.Parameters.Length == 0)
            return;

        var container = method.ContainingType?.OriginalDefinition;
        bool isInputRead =
            SymbolEqualityComparer.Default.Equals(container, known.BrickInput) && method.Name == "Get";
        bool isOutputWrite =
            SymbolEqualityComparer.Default.Equals(container, known.BrickOutput) && method.Name == "Set";

        if (!isInputRead && !isOutputWrite)
            return;

        var keyArgument = invocation.Arguments.FirstOrDefault(
            a => a.Parameter?.Name == "key")?.Value;
        if (keyArgument is null)
            return;

        // Only statically-known keys are judged. A computed or injected key
        // (variable, parameter, interpolation) is deliberately left alone —
        // the analyzer must never guess at a value it cannot see.
        var constant = keyArgument.ConstantValue;
        if (!constant.HasValue || constant.Value is not string key)
            return;

        var declared = isInputRead ? contract.Inputs : contract.Outputs;
        if (declared.Contains(key))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            isInputRead ? UndeclaredInput : UndeclaredOutput,
            keyArgument.Syntax.GetLocation(),
            brick.Name,
            key,
            declared.Count == 0 ? "(none)" : string.Join(", ", declared.OrderBy(d => d, StringComparer.Ordinal))));
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol candidate)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, candidate))
                return true;
        }

        return false;
    }

    /// <summary>The symbols the rule needs; absent any of them the rule cannot run.</summary>
    private sealed class KnownTypes
    {
        private KnownTypes(
            INamedTypeSymbol brick,
            INamedTypeSymbol brickInput,
            INamedTypeSymbol brickOutput,
            INamedTypeSymbol brickInterface,
            INamedTypeSymbol inputDefinition,
            INamedTypeSymbol outputDefinition)
        {
            Brick = brick;
            BrickInput = brickInput;
            BrickOutput = brickOutput;
            BrickInterface = brickInterface;
            InputDefinition = inputDefinition;
            OutputDefinition = outputDefinition;
        }

        public INamedTypeSymbol Brick { get; }
        public INamedTypeSymbol BrickInput { get; }
        public INamedTypeSymbol BrickOutput { get; }
        public INamedTypeSymbol BrickInterface { get; }
        public INamedTypeSymbol InputDefinition { get; }
        public INamedTypeSymbol OutputDefinition { get; }

        public static KnownTypes? From(Compilation compilation)
        {
            var brick = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Bricks.Brick");
            var input = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Execution.BrickInput");
            var output = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Execution.BrickOutput");
            var iface = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Bricks.BrickInterface");
            var inputDef = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Bricks.BrickInputDefinition");
            var outputDef = compilation.GetTypeByMetadataName("Nexo.Core.Domain.Bricks.BrickOutputDefinition");

            if (brick is null || input is null || output is null
                || iface is null || inputDef is null || outputDef is null)
            {
                return null;
            }

            return new KnownTypes(brick, input, output, iface, inputDef, outputDef);
        }
    }

    /// <summary>The input/output names a brick type declares in its own source.</summary>
    private sealed class BrickContract
    {
        private BrickContract(bool declared, ISet<string> inputs, ISet<string> outputs)
        {
            Declared = declared;
            Inputs = inputs;
            Outputs = outputs;
        }

        /// <summary>True when the type assigns a BrickInterface we could read.</summary>
        public bool Declared { get; }

        public ISet<string> Inputs { get; }
        public ISet<string> Outputs { get; }

        public static BrickContract Collect(
            INamedTypeSymbol type,
            KnownTypes known,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var inputs = new HashSet<string>(StringComparer.Ordinal);
            var outputs = new HashSet<string>(StringComparer.Ordinal);
            var declared = false;

            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var node = reference.GetSyntax(cancellationToken);
                if (!compilation.ContainsSyntaxTree(node.SyntaxTree))
                    continue;

                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var creation in node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    var created = model.GetTypeInfo(creation, cancellationToken).Type?.OriginalDefinition;
                    if (created is null)
                        continue;

                    if (SymbolEqualityComparer.Default.Equals(created, known.BrickInterface))
                    {
                        declared = true;
                        continue;
                    }

                    bool isInput = SymbolEqualityComparer.Default.Equals(created, known.InputDefinition);
                    bool isOutput = SymbolEqualityComparer.Default.Equals(created, known.OutputDefinition);
                    if (!isInput && !isOutput)
                        continue;

                    if (NameOf(creation, model, cancellationToken) is { } name)
                        (isInput ? inputs : outputs).Add(name);
                }
            }

            return new BrickContract(declared, inputs, outputs);
        }

        /// <summary>
        /// Reads the declared name out of <c>new BrickInputDefinition("target", …)</c>.
        ///
        /// The argument is located by the parameter it BINDS to rather than by
        /// position, so an out-of-order named form -- <c>new
        /// BrickOutputDefinition(type: "string", name: "result")</c> -- resolves
        /// correctly instead of silently reading the type as the name. The value
        /// is taken as a constant, so a <c>const</c> (the ProvenanceOutputKey
        /// pattern) is recognised exactly like a literal.
        /// </summary>
        private static string? NameOf(
            ObjectCreationExpressionSyntax creation,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            if (model.GetOperation(creation, cancellationToken) is not IObjectCreationOperation operation)
                return null;

            var nameArgument = operation.Arguments
                .FirstOrDefault(a => a.Parameter?.Name == "name")?.Value;

            return nameArgument?.ConstantValue is { HasValue: true, Value: string name }
                ? name
                : null;
        }
    }
}
