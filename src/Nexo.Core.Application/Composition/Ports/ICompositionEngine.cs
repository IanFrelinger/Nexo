using Nexo.Core.Application.Composition.Models;

namespace Nexo.Core.Application.Composition.Ports;

/// <summary>
/// Composes agents from capability components. Stub: returns pipeline spec for known problems.
/// </summary>
public interface ICompositionEngine
{
    Task<ComposedAgent?> ComposeAsync(string problemDescription, IReadOnlyList<string> availableCapabilities, CancellationToken cancellationToken = default);
}
