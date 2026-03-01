using Nexo.Core.Application.Composition.Models;
using Nexo.Core.Application.Composition.Ports;

namespace Nexo.Infrastructure.Composition;

/// <summary>
/// Stub composition engine. Given problem "test Nexo CLI", selects Perception + Validation + Reporting.
/// </summary>
public sealed class CompositionEngine : ICompositionEngine
{
    private readonly ICapabilityComponentRegistry _registry;

    public CompositionEngine(ICapabilityComponentRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public Task<ComposedAgent?> ComposeAsync(string problemDescription, IReadOnlyList<string> availableCapabilities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var desc = problemDescription.Trim().ToLowerInvariant();
        IReadOnlyList<string> componentIds;
        if (desc.Contains("test") && (desc.Contains("nexo") || desc.Contains("cli")))
            componentIds = new[] { "perception", "validation", "reporting" };
        else if (desc.Contains("test") && desc.Contains("failure"))
            componentIds = new[] { "perception", "code-analysis", "validation" };
        else if (desc.Contains("analyze") || desc.Contains("code"))
            componentIds = new[] { "code-analysis", "validation", "reporting" };
        else
            componentIds = new[] { "perception", "validation", "reporting" };

        var available = availableCapabilities.Count > 0 ? availableCapabilities : new[] { "perception", "validation", "reporting", "understanding", "code-analysis" };
        var filtered = componentIds.Where(id => available.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
        if (filtered.Count == 0)
            return Task.FromResult<ComposedAgent?>(null);

        return Task.FromResult<ComposedAgent?>(new ComposedAgent
        {
            Id = Guid.NewGuid().ToString("N"),
            ProblemDescription = problemDescription,
            ComponentIds = filtered,
        });
    }
}
