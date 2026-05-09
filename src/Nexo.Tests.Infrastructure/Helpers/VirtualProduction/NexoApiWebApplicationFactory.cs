using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Nexo.Tests.Infrastructure.Helpers.VirtualProduction;

/// <summary>
/// In-process <see cref="Nexo.API"/> host using the full ASP.NET Core pipeline and DI graph (same assembly as production).
/// Requests hit real endpoint delegates — no stubbed route handlers.
/// Environment <c>Testing</c> loads <c>appsettings.Testing.json</c> (background-agent hosted services disabled for deterministic CI).
/// </summary>
public sealed class NexoApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
