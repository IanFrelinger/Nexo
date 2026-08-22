using Microsoft.Extensions.DependencyInjection;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Ports;
using Ashlar.Orchestration.GameDomain.Agents.Assets;

namespace Ashlar.Orchestration.GameDomain;

/// <summary>
/// Supplies the generative asset agents: image, audio and 3D model.
///
/// <para>The split here is deliberate and narrower than it first looks. The asset PORTS —
/// <see cref="IImageGenerator"/>, <see cref="IAudioGenerator"/>,
/// <see cref="IModel3DGenerator"/>, <see cref="IAssetStorage"/> and the request/result types
/// — stay in the kernel, along with <c>BaseAssetAgent</c> and the <c>Generated*Asset</c>
/// models. None of them contain a word of game vocabulary; they are a general asset
/// generation capability.</para>
///
/// <para>Only these three concrete agents move, because their prompts are explicitly
/// game-flavoured: ImageAssetAgent is documented as "generates image assets for games" and
/// asks the model for "a high-quality game asset"; AudioAssetAgent asks for "high-quality
/// game audio" with "game context and consistency". The capability is general; this
/// particular framing of it is not.</para>
/// </summary>
public sealed class GameAssetAgentProvider : IDomainAgentProvider
{
    // Carried over verbatim from AgentFactory.IsAssetDomain. Note that "shader" and
    // "animation" are claimed here but have no case in Create below, so they throw — the
    // same advertise-then-reject shape as "telemetry" in PlaytestAgentProvider. Unlike that
    // one this behaviour IS covered by a kernel test (CreateAgent_unknown_asset_domain_throws
    // spawns "shader" and expects ArgumentException), so it is load-bearing, not merely
    // latent. Preserved exactly.
    private static readonly string[] Domains =
        { "image", "audio", "model3d", "3d", "model", "shader", "texture", "animation", "sound", "music" };

    /// <inheritdoc />
    public bool Handles(string domain) =>
        !string.IsNullOrWhiteSpace(domain) &&
        Array.Exists(Domains, d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public BaseAgent Create(AgentSpawnSpec spec, IAgentCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(context);

        var model = context.ResolveModel(spec);
        var storage = context.Services.GetRequiredService<IAssetStorage>();
        var baseLogger = context.BaseLogger;

        return spec.Domain.ToLowerInvariant() switch
        {
            "image" or "texture" => new ImageAssetAgent(
                spec,
                context.Services.GetRequiredService<IImageGenerator>(),
                model,
                storage,
                baseLogger),

            "audio" or "sound" or "music" => new AudioAssetAgent(
                spec,
                context.Services.GetRequiredService<IAudioGenerator>(),
                model,
                storage,
                baseLogger),

            "model3d" or "3d" or "model" => new Model3DAssetAgent(
                spec,
                context.Services.GetRequiredService<IModel3DGenerator>(),
                model,
                storage,
                baseLogger),

            _ => throw new ArgumentException($"Unknown asset domain: {spec.Domain}")
        };
    }
}
