using Ashlar.Core.Application.Composition.Models;

namespace Ashlar.Core.Application.Composition.Ports;

/// <summary>
/// Composes agents from capability components. Rule-based composition; matches problem descriptions to component pipelines.
/// </summary>
public interface ICompositionEngine
{
    Task<ComposedAgent?> ComposeAsync(string problemDescription, IReadOnlyList<string> availableCapabilities, CancellationToken cancellationToken = default);
}
