using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>Product tenant defaults included in support diagnostics export.</summary>
public sealed class SupportDiagnosticsProductSection
{
    /// <summary>Default tenant ID when the header is omitted.</summary>
    public string DefaultTenantId { get; init; } = "";

    /// <summary>Count of configured allowed tenant IDs.</summary>
    public int AllowedTenantCount { get; init; }
}
