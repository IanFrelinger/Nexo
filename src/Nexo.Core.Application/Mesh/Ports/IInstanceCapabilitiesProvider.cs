using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Provides the local instance's capabilities for mesh negotiation.
/// </summary>
public interface IInstanceCapabilitiesProvider
{
    /// <summary>
    /// Gets the capabilities of this instance.
    /// </summary>
    InstanceCapabilities GetCapabilities();
}
