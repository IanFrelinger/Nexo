using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Reference C++/EVTX profile registration surface. Supplies the deterministic
/// scaffold path as <see cref="IDeterministicDrafter"/>; full compile/sandbox
/// validators continue to live beside <see cref="CppParserAdapterBrick"/> during
/// the migration. Adding this profile does not require edits to
/// <c>GenerativeArtifactBrick</c>.
/// </summary>
public sealed class CppEvtxProfileFactory : IAgentProfileFactory
{
    /// <summary>Stable target id for the C++ EVTX adapter profile.</summary>
    public const string TargetId = "cpp-evtx";

    /// <inheritdoc />
    public AgentProfile Create(IServiceProvider services)
    {
        return new AgentProfile
        {
            TargetId = TargetId,
            Drafter = new AdapterScaffoldDrafter(),
            DeterministicDrafter = new AdapterScaffoldDrafter(),
            Validators = Array.Empty<object>(),
            Knowledge = new DomainKnowledge(),
            Llm = new LLMConfig
            {
                Model = "local",
                SystemPrompt = "Draft an IEventReader adapter against the extracted parser surface."
            },
            Tunables = new GenerationTunables
            {
                PreferDeterministic = true,
                MaxRepairAttempts = 3
            },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsSandbox = true,
                SupportsDeployment = true
            }
        };
    }
}

/// <summary>
/// Thin <see cref="IArtifactDrafter"/> / <see cref="IDeterministicDrafter"/> over
/// <see cref="AdapterScaffold"/> for profile registration. Full model drafting
/// remains on <see cref="CppParserAdapterBrick"/> until that brick is retired.
/// </summary>
internal sealed class AdapterScaffoldDrafter : IArtifactDrafter, IDeterministicDrafter
{
    public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
    {
        artifact = null;
        // Scaffold needs a real parser surface; without it, decline so the
        // model/legacy brick path can take over.
        if (string.IsNullOrWhiteSpace(request.Grounding))
            return false;

        var scaffold = AdapterScaffold.TryBuild(request.Grounding);
        if (scaffold is null)
            return false;

        artifact = new GeneratedArtifact { Content = scaffold };
        return true;
    }

    public Task<GeneratedArtifact> DraftAsync(
        GenerationRequest request,
        IReadOnlyList<string> priorFeedback,
        CancellationToken cancellationToken = default)
    {
        if (TryDraft(request, out var artifact) && artifact is not null)
            return Task.FromResult(artifact);
        return Task.FromResult(new GeneratedArtifact
        {
            Content = "// scaffold unavailable — use CppParserAdapterBrick model path"
        });
    }
}
