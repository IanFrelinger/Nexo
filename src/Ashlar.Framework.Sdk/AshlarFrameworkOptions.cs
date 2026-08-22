using Microsoft.Extensions.DependencyInjection;
using Ashlar.Hosting;

namespace Ashlar.Framework.Sdk;

/// <summary>
/// Single visible option bag for apps that want client + host wiring without importing multiple extension namespaces.
/// </summary>
public sealed class AshlarFrameworkOptions
{
    /// <summary>
    /// When set, registers <c>AddAshlarClientSdk</c> from <c>Ashlar.Sdk.Client</c>.
    /// Remote-only apps can skip <see cref="RegisterKernel"/>.
    /// </summary>
    public string? RemoteApiBaseUrl { get; set; }

    /// Forwarded to Hosting <c>AddAshlar</c>.
    public Action<AshlarHostingOptions>? ConfigureHost { get; set; }

    /// <summary>When true (default), registers the full Ashlar kernel via <c>AddAshlar</c>.</summary>
    public bool RegisterKernel { get; set; } = true;
}
