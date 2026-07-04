using Nexo.Abstractions;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Sdk.Ports;

namespace Nexo.Hosting.Sdk.Builders;

/// <summary>
/// Back-compat type name for <see cref="HostNexoSdkBuilder"/>.
/// </summary>
[Obsolete("Renamed to HostNexoSdkBuilder.", error: false)]
public sealed class NexoSdkBuilder : HostNexoSdkBuilder
{
    /// <inheritdoc cref="HostNexoSdkBuilder(NexoSdkOptions)"/>
    public NexoSdkBuilder(NexoSdkOptions options)
        : base(options)
    {
    }
}
