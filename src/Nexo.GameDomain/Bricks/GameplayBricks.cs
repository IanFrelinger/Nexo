using Nexo.Core.Domain.Bricks;

namespace Nexo.GameDomain.Bricks;

/// <summary>Factory for the default open-core gameplay brick set.</summary>
public static class GameplayBricks
{
    /// <summary>Creates the default deterministic gameplay bricks.</summary>
    public static IReadOnlyList<DomainBrick> CreateDefaults()
        =>
        [
            new DamageResolverBrick(),
            new AbilityGateBrick(),
            new ScoreAggregateBrick()
        ];
}
