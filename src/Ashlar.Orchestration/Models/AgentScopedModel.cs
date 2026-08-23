using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;

namespace Ashlar.Orchestration.Models;

/// <summary>
/// Agent-scoped model wrapper: injects agent/domain tags + per-agent/domain runtime spec directives.
/// </summary>
public sealed class AgentScopedModel : IModel
{
    private readonly IModel _inner;
    private readonly string _agentId;
    private readonly string _domain;
    private readonly ModelRuntimeSpec _runtime;

    public AgentScopedModel(IModel inner, string agentId, string domain, ModelRuntimeSpec runtime)
    {
        _inner = inner;
        _agentId = agentId;
        _domain = domain;
        _runtime = runtime;
    }

    /// <summary>Completes a model request with agent and domain runtime directives injected into the system message.</summary>
    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var header = $"ashlar.agent.id={_agentId}\n" +
                     $"ashlar.agent.domain={_domain}\n" +
                     $"ashlar.model.prefer={_runtime.Prefer}\n" +
                     (string.IsNullOrWhiteSpace(_runtime.Provider) ? "" : $"ashlar.model.provider={_runtime.Provider}\n") +
                     (string.IsNullOrWhiteSpace(_runtime.Model) ? "" : $"ashlar.model.name={_runtime.Model}\n");

        var msgs = input.Messages.ToList();
        for (var i = 0; i < msgs.Count; i++)
        {
            var (role, content) = msgs[i];
            if (!string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)) continue;
            msgs[i] = (role, header + (content ?? ""));
            return _inner.CompleteAsync(new ModelInput(msgs), ct);
        }

        msgs.Insert(0, ("system", header));
        return _inner.CompleteAsync(new ModelInput(msgs), ct);
    }
}
