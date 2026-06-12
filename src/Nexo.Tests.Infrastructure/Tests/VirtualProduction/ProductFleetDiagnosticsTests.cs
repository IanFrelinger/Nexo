using System.Net.Http.Json;
using FluentAssertions;
using Nexo.API.Security;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ProductFleetDiagnosticsTests : IClassFixture<NexoApiWebApplicationFactory>
{
    private readonly NexoApiWebApplicationFactory _factory;

    public ProductFleetDiagnosticsTests(NexoApiWebApplicationFactory factory) => _factory = factory;

    [Fact(Timeout = 120000)]
    public async Task GET_support_diagnostics_returns_redacted_bundle()
    {
        var client = _factory.CreateClient();
        using var response = await client.GetAsync("api/support/diagnostics");
        response.EnsureSuccessStatusCode();

        var diagnostics = await response.Content.ReadFromJsonAsync<SupportDiagnosticsResponse>();
        diagnostics.Should().NotBeNull();
        diagnostics!.Application.Should().Be("Nexo.API");
        diagnostics.Security.ApiKeyConfigured.Should().BeFalse();
        diagnostics.License.State.Should().Be(nameof(PrivateLicenseState.NotConfigured));
        diagnostics.RedactedConfiguration.Should().NotBeEmpty();
        diagnostics.RedactedConfiguration.Values.Should().NotContain("secret-key");
    }
}
