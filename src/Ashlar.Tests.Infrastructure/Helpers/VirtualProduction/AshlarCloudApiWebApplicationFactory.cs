using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;

/// <summary>API host with <see cref="Ashlar.API.Security.AshlarProductOptions.RequireOrgMembership"/> enabled.</summary>
public sealed class AshlarCloudApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ashlar:Product:RequireOrgMembership"] = "true",
            });
        });
    }
}
