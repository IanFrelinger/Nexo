using System.Collections.Concurrent;
using Nexo.Core.Application.Composition.Models;
using Nexo.Core.Application.Composition.Ports;

namespace Nexo.Infrastructure.Composition;

/// <summary>
/// In-memory composition cache.
/// </summary>
public sealed class InMemoryCompositionCache : ICompositionCache
{
    private readonly ConcurrentDictionary<string, (ComposedAgent Agent, bool Validated)> _cache = new();

    /// <inheritdoc />
    public Task<bool> TryGetAsync(string problemKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.ContainsKey(problemKey));
    }

    /// <inheritdoc />
    public Task StoreAsync(ComposedAgent agent, bool validated, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = agent.ProblemDescription.Trim().ToLowerInvariant().GetHashCode().ToString();
        _cache[key] = (agent, validated);
        return Task.CompletedTask;
    }
}
