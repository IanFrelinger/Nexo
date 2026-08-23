using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Ashlar.Analyzers;

/// <summary>
/// Flags the statically-nameable determinism violations in brick code: wall-clock reads
/// (<c>DateTime.Now</c>/<c>DateTimeOffset.Now</c>), unseeded randomness (<c>new Random()</c>,
/// <c>Random.Shared</c>), and mutable static state.
///
/// The certification chain's determinism gate executes a brick twice and rejects divergent
/// outputs — but that is an expensive gate discovering what a compiler could have named.
/// These rules convert the three most common divergence sources into immediate, located,
/// causal diagnostics.
///
/// Honesty discipline: only exact symbol matches are flagged. <c>DateTime.UtcNow</c> is
/// legal (the rule requires Utc, not the absence of time). A seeded <c>new Random(seed)</c>
/// is legal. A <c>static readonly</c> field is left alone even when its type is a mutable
/// container — whether a container is mutated cannot be resolved statically without call-graph
/// guessing, and this catalog never guesses.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrickDeterminismAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic id for local-time wall-clock reads.</summary>
    public const string WallClockId = "ASHLAR0006";

    /// <summary>Diagnostic id for unseeded randomness.</summary>
    public const string UnseededRandomId = "ASHLAR0007";

    /// <summary>Diagnostic id for mutable static state.</summary>
    public const string MutableStaticId = "ASHLAR0008";

    private const string Category = "Ashlar.TrustLoop";

    private static readonly DiagnosticDescriptor WallClock = new(
        WallClockId,
        title: "Brick reads local wall-clock time",
        messageFormat:
            "Brick '{0}' reads {1}, which depends on host time zone and the moment of execution. "
            + "Two runs with identical inputs produce different outputs, so the determinism gate will "
            + "reject the brick — or worse, pass flakily. Use {2} if time truly is an input, and "
            + "declare it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick outputs must be a function of declared inputs. Local wall-clock reads make output "
            + "depend on execution time and host time zone.");

    private static readonly DiagnosticDescriptor UnseededRandom = new(
        UnseededRandomId,
        title: "Brick uses unseeded randomness",
        messageFormat:
            "Brick '{0}' uses {1}, which is seeded from the environment. Two runs with identical "
            + "inputs produce different outputs, so the determinism gate will reject the brick. Take a "
            + "seed as a declared input and construct Random from it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick outputs must be a function of declared inputs. Unseeded randomness makes output "
            + "depend on the environment at execution time.");

    private static readonly DiagnosticDescriptor MutableStatic = new(
        MutableStaticId,
        title: "Brick declares mutable static state",
        messageFormat:
            "Brick '{0}' declares mutable static field '{1}'. State that survives across executions "
            + "makes output depend on invocation history: the determinism gate's second run sees a "
            + "different world than its first. Make it const, readonly-immutable, or an instance field "
            + "fed from declared inputs.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick outputs must be a function of declared inputs. Mutable static state carries "
            + "information between executions.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(WallClock, UnseededRandom, MutableStatic);

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

        var dateTime = context.Compilation.GetTypeByMetadataName("System.DateTime");
        var dateTimeOffset = context.Compilation.GetTypeByMetadataName("System.DateTimeOffset");
        var random = context.Compilation.GetTypeByMetadataName("System.Random");

        context.RegisterSymbolStartAction(
            symbolStart =>
            {
                var type = (INamedTypeSymbol)symbolStart.Symbol;
                if (!BrickTypeResolution.IsConcreteBrick(type, brickBase))
                    return;

                symbolStart.RegisterOperationAction(
                    operation => OnPropertyReference(operation, type, dateTime, dateTimeOffset, random),
                    OperationKind.PropertyReference);
                symbolStart.RegisterOperationAction(
                    operation => OnObjectCreation(operation, type, random),
                    OperationKind.ObjectCreation);
                symbolStart.RegisterSymbolEndAction(symbolEnd => OnSymbolEnd(symbolEnd, type));
            },
            SymbolKind.NamedType);
    }

    private static void OnPropertyReference(
        OperationAnalysisContext context,
        INamedTypeSymbol brick,
        INamedTypeSymbol? dateTime,
        INamedTypeSymbol? dateTimeOffset,
        INamedTypeSymbol? random)
    {
        var property = ((IPropertyReferenceOperation)context.Operation).Property;
        var containing = property.ContainingType?.OriginalDefinition;
        if (containing is null)
            return;

        if (property.Name == "Now" &&
            (SymbolEquals(containing, dateTime) || SymbolEquals(containing, dateTimeOffset)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                WallClock,
                context.Operation.Syntax.GetLocation(),
                brick.Name,
                $"{containing.Name}.Now",
                $"{containing.Name}.UtcNow"));
            return;
        }

        if (property.Name == "Shared" && SymbolEquals(containing, random))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UnseededRandom,
                context.Operation.Syntax.GetLocation(),
                brick.Name,
                "Random.Shared"));
        }
    }

    private static void OnObjectCreation(
        OperationAnalysisContext context,
        INamedTypeSymbol brick,
        INamedTypeSymbol? random)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        if (creation.Arguments.Length == 0 &&
            creation.Type?.OriginalDefinition is INamedTypeSymbol created &&
            SymbolEquals(created, random))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UnseededRandom,
                context.Operation.Syntax.GetLocation(),
                brick.Name,
                "new Random() without a seed"));
        }
    }

    private static void OnSymbolEnd(SymbolAnalysisContext context, INamedTypeSymbol brick)
    {
        foreach (var member in brick.GetMembers())
        {
            if (member is IFieldSymbol
                {
                    IsStatic: true,
                    IsConst: false,
                    IsReadOnly: false,
                    IsImplicitlyDeclared: false,
                } field)
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MutableStatic,
                        location,
                        brick.Name,
                        field.Name));
                }
            }
        }
    }

    private static bool SymbolEquals(INamedTypeSymbol left, INamedTypeSymbol? right)
        => right is not null && SymbolEqualityComparer.Default.Equals(left, right);
}
