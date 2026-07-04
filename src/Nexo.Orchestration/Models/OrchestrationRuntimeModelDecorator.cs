using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.Orchestration.Models;

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

    /// <summary>Completes a request after injecting runtime spec directives into the system message.</summary>
    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var rt = _spec.Current.Model;
        if (rt is { Prefer: "auto" } && string.IsNullOrWhiteSpace(rt.Provider))
        {
            return _inner.CompleteAsync(input, ct);
        }

        var routed = InjectDirectives(input, rt.Prefer, rt.Provider, rt.Model);
        return _inner.CompleteAsync(routed, ct);
    }

    /// <summary>Injects nexo.model preference directives into the first system message.</summary>
    public static ModelInput InjectDirectives(ModelInput input, string? prefer, string? provider, string? model)
    {
        var directives = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefer)) directives.Add($"nexo.model.prefer={prefer}");
        if (!string.IsNullOrWhiteSpace(provider)) directives.Add($"nexo.model.provider={provider}");
        if (!string.IsNullOrWhiteSpace(model)) directives.Add($"nexo.model.name={model}");
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
