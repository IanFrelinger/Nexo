using Microsoft.CodeAnalysis;

namespace Nexo.Analyzers;

/// <summary>
/// Shared brick-type anchoring for the trust-loop analyzer catalog.
///
/// Every brick-scoped rule gates on the same two facts: the compilation can resolve the
/// canonical <c>Nexo.Core.Domain.Bricks.Brick</c> base type, and the inspected type derives
/// from it. When the anchor cannot be resolved the rules stay silent — the honesty
/// discipline: never judge code against a contract the compilation cannot see. (The
/// certification analyzer gate separately fails-closed when anchors are missing from a
/// candidate compilation; that check lives in the gate, not here, so IDE builds of unrelated
/// code are not spammed.)
/// </summary>
internal static class BrickTypeResolution
{
    /// <summary>Metadata name of the canonical brick base type.</summary>
    public const string BrickMetadataName = "Nexo.Core.Domain.Bricks.Brick";

    /// <summary>Resolves the brick base type, or null when this compilation cannot see it.</summary>
    public static INamedTypeSymbol? ResolveBrickBase(Compilation compilation)
        => compilation.GetTypeByMetadataName(BrickMetadataName);

    /// <summary>Whether <paramref name="type"/> is a concrete class deriving from the brick base.</summary>
    public static bool IsConcreteBrick(INamedTypeSymbol type, INamedTypeSymbol brickBase)
    {
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
            return false;

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, brickBase))
                return true;
        }

        return false;
    }
}
