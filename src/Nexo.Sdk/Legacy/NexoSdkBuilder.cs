#pragma warning disable CS0618 // intentional obsolete forwarding surface for NuGet compatibility

using Microsoft.Extensions.DependencyInjection;
using Nexo.Sdk.Client;

// Namespace is deliberately Nexo.Sdk, NOT Nexo.Sdk.Legacy: this type exists so
// that existing `using Nexo.Sdk;` code keeps compiling. Moving it into a .Legacy
// namespace breaks exactly the consumers the [Obsolete] shim was added to
// protect, which defeats its entire purpose. The file lives under Legacy/ for
// organisation only — the namespace is the contract.
namespace Nexo.Sdk;

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
