#pragma warning disable CS0618 // intentional obsolete forwarding surface for NuGet compatibility

using Microsoft.Extensions.DependencyInjection;
using Nexo.Sdk.Client;

namespace Nexo.Sdk.Legacy;
/// <summary>
/// Back-compat names for the HTTP client SDK. Prefer <see cref="NexoClientSdkBuilder"/> and
/// <see cref="NexoClientSdkServiceCollectionExtensions.AddNexoClientSdk"/>.
/// </summary>
[Obsolete("Renamed to NexoClientSdkBuilder (namespace Nexo.Sdk.Client).", error: false)]
public sealed class NexoSdkBuilder : NexoClientSdkBuilder
{
    internal NexoSdkBuilder(IServiceCollection services)
        : base(services)
    {
    }
}
