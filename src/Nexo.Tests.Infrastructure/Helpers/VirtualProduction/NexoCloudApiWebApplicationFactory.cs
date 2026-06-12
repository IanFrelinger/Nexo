using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Nexo.Tests.Infrastructure.Helpers.VirtualProduction;

/// <summary>API host with <see cref="Nexo.API.Security.NexoProductOptions.RequireOrgMembership"/> enabled.</summary>
public sealed class NexoCloudApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Product:RequireOrgMembership"] = "true",
            });
        });
    }
}
