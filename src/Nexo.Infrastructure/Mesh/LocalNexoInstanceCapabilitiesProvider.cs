using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// Provides local Nexo instance capabilities: can compile, supports Source/Binary/Config.
/// </summary>
public sealed class LocalNexoInstanceCapabilitiesProvider : IInstanceCapabilitiesProvider
{
    /// <inheritdoc />
    public InstanceCapabilities GetCapabilities() => InstanceCapabilities.LocalNexo;
}
