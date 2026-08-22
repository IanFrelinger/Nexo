using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Core.Application.Mesh.Ports;

/// <summary>
/// Provides the local instance's capabilities for mesh negotiation.
/// </summary>
public interface IInstanceCapabilitiesProvider
{
    /// <summary>
    /// Gets the capabilities of this instance.
    /// </summary>
    /// <returns>Supported artifact formats and runtime flags.</returns>
    InstanceCapabilities GetCapabilities();
}
