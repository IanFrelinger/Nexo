using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Nexo.Skills.AgentFramework;

/// <summary>
/// Minimal agent stub used only to satisfy AgentSkillsSourceContext for catalog operations.
/// </summary>
internal sealed class CatalogSourceAgent : AIAgent
{
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new CatalogSourceSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(default(JsonElement));

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new CatalogSourceSession());

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Catalog stub agent cannot run chat requests.");

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Catalog stub agent cannot run chat requests.");
}

internal sealed class CatalogSourceSession : AgentSession
{
}

internal static class CatalogSourceContextFactory
{
    private static readonly ConditionalWeakTable<IServiceProvider, AgentSkillsSourceContext> Contexts = new();

    public static AgentSkillsSourceContext Create(IServiceProvider? serviceProvider = null)
    {
        if (serviceProvider == null)
            return new AgentSkillsSourceContext(new CatalogSourceAgent(), new CatalogSourceSession());

        return Contexts.GetValue(serviceProvider, static _ =>
            new AgentSkillsSourceContext(new CatalogSourceAgent(), new CatalogSourceSession()));
    }
}
