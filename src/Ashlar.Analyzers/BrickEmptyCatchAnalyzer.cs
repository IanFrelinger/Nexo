using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Ashlar.Analyzers;

/// <summary>
/// Flags empty catch blocks in brick code.
///
/// A brick that swallows an exception converts an explainable failure into silent wrong
/// output: the certification chain's correctness witness may pass on the happy path while the
/// deployed brick quietly produces defaults on the failure path — the exact "no exception, no
/// failing test" slop class the trust loop exists to eliminate.
///
/// Honesty discipline: only a catch block containing zero statements is flagged. A catch
/// that does anything — logs, wraps, records, sets a fallback, even discards to a named
/// variable — is a judgement call this analyzer does not second-guess.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrickEmptyCatchAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic id for empty catch blocks in brick execution paths.</summary>
    public const string EmptyCatchId = "ASHLAR0009";

    private const string Category = "Ashlar.TrustLoop";

    private static readonly DiagnosticDescriptor EmptyCatch = new(
        EmptyCatchId,
        title: "Brick swallows exceptions in an empty catch block",
        messageFormat:
            "Brick '{0}' has an empty catch block. A failure on this path becomes silent wrong "
            + "output instead of an explained failure — the caller sees defaults and nothing sees the "
            + "exception. Rethrow, wrap with context, or record the failure into the brick's output.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick failures must be explained, not swallowed. An empty catch block converts an "
            + "attributable failure into silent wrong output.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(EmptyCatch);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var brickBase = BrickTypeResolution.ResolveBrickBase(context.Compilation);
        if (brickBase is null)
            return;

        context.RegisterSymbolStartAction(
            symbolStart =>
            {
                var type = (INamedTypeSymbol)symbolStart.Symbol;
                if (!BrickTypeResolution.IsConcreteBrick(type, brickBase))
                    return;

                symbolStart.RegisterOperationAction(
                    operation => OnCatchClause(operation, type),
                    OperationKind.CatchClause);
            },
            SymbolKind.NamedType);
    }

    private static void OnCatchClause(OperationAnalysisContext context, INamedTypeSymbol brick)
    {
        var catchClause = (ICatchClauseOperation)context.Operation;
        if (catchClause.Handler.Operations.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EmptyCatch,
                catchClause.Syntax.GetLocation(),
                brick.Name));
        }
    }
}
