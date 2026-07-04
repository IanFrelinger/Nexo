using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Domain.Bricks;
using Nexo.Hosting.Sdk.Extensions;

namespace Nexo.Authoring;

/// <summary>
/// Stable registration helpers for code-authored Nexo bricks.
/// </summary>
public static class NexoAuthoringServiceCollectionExtensions
{
    /// <summary>
    /// Registers a code-authored brick with the Nexo host SDK.
    /// Call before <c>AddNexo()</c>.
    /// </summary>
    public static IServiceCollection AddNexoBrick<TBrick>(this IServiceCollection services)
        where TBrick : DomainBrick
    {
        return services.AddNexoSdk(sdk => sdk.RegisterBrick<TBrick>());
    }
}
