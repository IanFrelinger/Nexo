using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.Orchestration.Models;

public sealed record OrchestrationRuntimeSpec
{
    public ModelRuntimeSpec Model { get; init; } = new();
    public Dictionary<string, ModelRuntimeSpec> Domains { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ModelRuntimeSpec> Agents { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? BarrierLevel { get; init; }
    public string? PreferredRegion { get; init; }

    public ModelRuntimeSpec Resolve(string agentId, string domain)
    {
        if (!string.IsNullOrWhiteSpace(agentId) && Agents.TryGetValue(agentId, out var a)) return a;
        if (!string.IsNullOrWhiteSpace(domain) && Domains.TryGetValue(domain, out var d)) return d;
        return Model;
    }

    public static OrchestrationRuntimeSpec Default() => new();
}

public sealed record ModelRuntimeSpec
{
    /// <summary> "agentic", "deterministic", or "auto" </summary>
    public string Prefer { get; init; } = "auto";
    /// <summary> Provider name (openai/azure/ollama/offline/mock-json/...) </summary>
    public string? Provider { get; init; }
}

public interface IOrchestrationRuntimeSpecAccessor
{
    OrchestrationRuntimeSpec Current { get; }
    IDisposable Begin(OrchestrationRuntimeSpec spec);
}

public sealed class OrchestrationRuntimeSpecAccessor : IOrchestrationRuntimeSpecAccessor
{
    private static readonly AsyncLocal<OrchestrationRuntimeSpec?> Ambient = new();

    public OrchestrationRuntimeSpec Current => Ambient.Value ?? OrchestrationRuntimeSpec.Default();

    public IDisposable Begin(OrchestrationRuntimeSpec spec)
    {
        var prior = Ambient.Value;
        Ambient.Value = spec;
        return new Scope(() => Ambient.Value = prior);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _dispose;
        private bool _done;
        public Scope(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            if (_done) return;
            _done = true;
            _dispose();
        }
    }
}

/// <summary>
/// Decorates an IModel so orchestration-wide runtime spec can be applied without touching every callsite.
/// It injects nexo.model.prefer/provider directives based on the current runtime spec (default scope).
/// </summary>
public sealed class OrchestrationRuntimeModelDecorator : IModel
{
    private readonly IModel _inner;
    private readonly IOrchestrationRuntimeSpecAccessor _spec;
    private readonly ILogger<OrchestrationRuntimeModelDecorator> _logger;

    public OrchestrationRuntimeModelDecorator(
        IModel inner,
        IOrchestrationRuntimeSpecAccessor spec,
        ILogger<OrchestrationRuntimeModelDecorator> logger)
    {
        _inner = inner;
        _spec = spec;
        _logger = logger;
    }

    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var rt = _spec.Current.Model;
        if (rt is { Prefer: "auto" } && string.IsNullOrWhiteSpace(rt.Provider))
        {
            return _inner.CompleteAsync(input, ct);
        }

        var routed = InjectDirectives(input, rt.Prefer, rt.Provider);
        return _inner.CompleteAsync(routed, ct);
    }

    public static ModelInput InjectDirectives(ModelInput input, string? prefer, string? provider)
    {
        var directives = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefer)) directives.Add($"nexo.model.prefer={prefer}");
        if (!string.IsNullOrWhiteSpace(provider)) directives.Add($"nexo.model.provider={provider}");
        if (directives.Count == 0) return input;

        var header = string.Join("\n", directives);
        var msgs = input.Messages.ToList();

        for (var i = 0; i < msgs.Count; i++)
        {
            var (role, content) = msgs[i];
            if (!string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)) continue;
            msgs[i] = (role, header + "\n" + (content ?? ""));
            return new ModelInput(msgs);
        }

        msgs.Insert(0, ("system", header));
        return new ModelInput(msgs);
    }
}

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

    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var header = $"nexo.agent.id={_agentId}\n" +
                     $"nexo.agent.domain={_domain}\n" +
                     $"nexo.model.prefer={_runtime.Prefer}\n" +
                     (string.IsNullOrWhiteSpace(_runtime.Provider) ? "" : $"nexo.model.provider={_runtime.Provider}\n");

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

