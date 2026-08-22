using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;

namespace Ashlar.Infrastructure.Mesh;

/// <summary>
/// Provides local Ashlar instance capabilities: can compile, supports Source/Binary/Config.
/// </summary>
public sealed class LocalAshlarInstanceCapabilitiesProvider : IInstanceCapabilitiesProvider
{
    /// <inheritdoc />
    public InstanceCapabilities GetCapabilities() => InstanceCapabilities.LocalAshlar;
}
