using Ashlar.Abstractions;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Sdk.Ports;

namespace Ashlar.Hosting.Sdk.Builders;

/// <summary>
/// Back-compat type name for <see cref="HostAshlarSdkBuilder"/>.
/// </summary>
[Obsolete("Renamed to HostAshlarSdkBuilder.", error: false)]
public sealed class AshlarSdkBuilder : HostAshlarSdkBuilder
{
    /// <inheritdoc cref="HostAshlarSdkBuilder(AshlarSdkOptions)"/>
    public AshlarSdkBuilder(AshlarSdkOptions options)
        : base(options)
    {
    }
}
