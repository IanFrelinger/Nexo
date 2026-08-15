using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// The structural constraints <see cref="ProviderGeneratorModel"/> used to state as
/// prose, now as one machine-checkable manifest (trust-loop spec R3.5): the same
/// object renders the proposer instructions and backs ingest enforcement, so prompt
/// and gate cannot drift.
/// </summary>
public static class DamageResolverBrickConstraints
{
    /// <summary>The manifest for generated DamageResolver-domain bricks.</summary>
    public static BrickConstraintManifest Default { get; } = new()
    {
        RequiredBaseType = "DomainBrick",
        AllowedUsings = new[]
        {
            "Nexo.Core.Domain.Bricks",
            "Nexo.Core.Domain.Execution"
        },
        RequiredNamespace = "Nexo.Certified.DamageResolver",
        RequiredClassNameSuffix = "Brick",
        ForbiddenApiTokens = new[]
        {
            "DateTime.Now",
            "DateTime.UtcNow",
            "Random",
            "Guid.NewGuid",
            "Environment.TickCount"
        },
        RequiredDeclarations = new[]
        {
            "ExecuteAsync"
        }
    };
}
