using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Ashlar.Analyzers;

/// <summary>
/// Flags file-system and network side effects in brick constructors and field initializers.
///
/// Brick constructors run at registration time and during hot-swap generation
/// materialization — contexts where a side effect is unattributable, unbudgeted, and outside
/// every gate. A constructor that opens files or sockets turns "register a brick" into an
/// I/O operation whose failure surfaces as a DI or swap refusal with no repair feedback.
///
/// Honesty discipline: only invocations and object creations whose target symbol resolves to
/// a known I/O or network type are flagged. Banned types the compilation cannot resolve are
/// simply not banned here; indirection through helper methods is left alone (the analyzer
/// does not chase call graphs — a helper call it cannot see into is not judged).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrickConstructorPurityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic id for constructor file-system I/O.</summary>
    public const string ConstructorIoId = "ASHLAR0003";

    /// <summary>Diagnostic id for constructor network access.</summary>
    public const string ConstructorNetworkId = "ASHLAR0004";

    private const string Category = "Ashlar.TrustLoop";

    private static readonly DiagnosticDescriptor ConstructorIo = new(
        ConstructorIoId,
        title: "Brick constructor performs file-system I/O",
        messageFormat:
            "Brick '{0}' touches the file system in {1} via '{2}'. Constructors run during "
            + "registration and hot-swap materialization, where I/O is unattributable and outside every "
            + "gate; move the work into ExecuteAsync so it is budgeted, sandboxed, and repairable.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick construction must be pure: file-system access belongs in ExecuteAsync where the "
            + "trust loop can budget, sandbox, and attribute it.");

    private static readonly DiagnosticDescriptor ConstructorNetwork = new(
        ConstructorNetworkId,
        title: "Brick constructor performs network access",
        messageFormat:
            "Brick '{0}' opens network resources in {1} via '{2}'. Constructors run during "
            + "registration and hot-swap materialization, where egress bypasses the session's network "
            + "policy; move the work into ExecuteAsync so the sandbox's network mode governs it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Brick construction must be pure: network access belongs in ExecuteAsync where the "
            + "session's declared network mode is enforced.");

    private static readonly ImmutableArray<string> IoInvocationTypes = ImmutableArray.Create(
        "System.IO.File",
        "System.IO.Directory");

    private static readonly ImmutableArray<string> IoCreationTypes = ImmutableArray.Create(
        "System.IO.FileStream",
        "System.IO.StreamReader",
        "System.IO.StreamWriter",
        "System.IO.FileInfo",
        "System.IO.DirectoryInfo");

    private static readonly ImmutableArray<string> NetworkInvocationTypes = ImmutableArray.Create(
        "System.Net.WebRequest",
        "System.Net.Dns");

    private static readonly ImmutableArray<string> NetworkCreationTypes = ImmutableArray.Create(
        "System.Net.Http.HttpClient",
        "System.Net.Sockets.Socket",
        "System.Net.Sockets.TcpClient",
        "System.Net.Sockets.UdpClient");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ConstructorIo, ConstructorNetwork);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private sealed class BannedTypes
    {
        public required ImmutableHashSet<INamedTypeSymbol> IoInvocation { get; init; }
        public required ImmutableHashSet<INamedTypeSymbol> IoCreation { get; init; }
        public required ImmutableHashSet<INamedTypeSymbol> NetworkInvocation { get; init; }
        public required ImmutableHashSet<INamedTypeSymbol> NetworkCreation { get; init; }
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var brickBase = BrickTypeResolution.ResolveBrickBase(context.Compilation);
        if (brickBase is null)
            return;

        var banned = new BannedTypes
        {
            IoInvocation = Resolve(context.Compilation, IoInvocationTypes),
            IoCreation = Resolve(context.Compilation, IoCreationTypes),
            NetworkInvocation = Resolve(context.Compilation, NetworkInvocationTypes),
            NetworkCreation = Resolve(context.Compilation, NetworkCreationTypes),
        };

        context.RegisterSymbolStartAction(
            symbolStart =>
            {
                var type = (INamedTypeSymbol)symbolStart.Symbol;
                if (!BrickTypeResolution.IsConcreteBrick(type, brickBase))
                    return;

                symbolStart.RegisterOperationAction(
                    operation => OnOperation(operation, banned, type),
                    OperationKind.Invocation,
                    OperationKind.ObjectCreation);
            },
            SymbolKind.NamedType);
    }

    private static ImmutableHashSet<INamedTypeSymbol> Resolve(Compilation compilation, ImmutableArray<string> names)
    {
        var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var name in names)
        {
            if (compilation.GetTypeByMetadataName(name) is { } symbol)
                builder.Add(symbol);
        }

        return builder.ToImmutable();
    }

    private static void OnOperation(OperationAnalysisContext context, BannedTypes banned, INamedTypeSymbol brick)
    {
        // Only construction-time execution paths: instance constructors and instance
        // field/property initializers (which run as part of construction).
        if (!IsConstructionContext(context.ContainingSymbol, out var where))
            return;

        switch (context.Operation)
        {
            case IInvocationOperation invocation:
            {
                var containing = invocation.TargetMethod.ContainingType?.OriginalDefinition;
                if (containing is null)
                    return;

                if (banned.IoInvocation.Contains(containing))
                    Report(context, ConstructorIo, brick, where, Describe(containing, invocation.TargetMethod.Name));
                else if (banned.NetworkInvocation.Contains(containing))
                    Report(context, ConstructorNetwork, brick, where, Describe(containing, invocation.TargetMethod.Name));
                return;
            }

            case IObjectCreationOperation creation:
            {
                if (creation.Type?.OriginalDefinition is not INamedTypeSymbol created)
                    return;

                if (banned.IoCreation.Contains(created))
                    Report(context, ConstructorIo, brick, where, $"new {created.Name}(...)");
                else if (banned.NetworkCreation.Contains(created))
                    Report(context, ConstructorNetwork, brick, where, $"new {created.Name}(...)");
                return;
            }
        }
    }

    private static bool IsConstructionContext(ISymbol containingSymbol, out string where)
    {
        switch (containingSymbol)
        {
            case IMethodSymbol { MethodKind: MethodKind.Constructor, IsStatic: false }:
                where = "its constructor";
                return true;
            case IFieldSymbol { IsStatic: false }:
                where = "a field initializer";
                return true;
            case IPropertySymbol { IsStatic: false }:
                where = "a property initializer";
                return true;
            default:
                where = string.Empty;
                return false;
        }
    }

    private static string Describe(INamedTypeSymbol type, string member) => $"{type.Name}.{member}";

    private static void Report(
        OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        INamedTypeSymbol brick,
        string where,
        string api)
        => context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            context.Operation.Syntax.GetLocation(),
            brick.Name,
            where,
            api));
}
