using System.Net.Http.Json;
using FluentAssertions;
using Ashlar.API.Security;
using Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>Tests for product fleet diagnostics.</summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ProductFleetDiagnosticsTests : IClassFixture<AshlarApiWebApplicationFactory>
{
    private readonly AshlarApiWebApplicationFactory _factory;

    /// <summary>Product fleet diagnostics tests.</summary>
    /// <param name="factory">Factory.</param>
    public ProductFleetDiagnosticsTests(AshlarApiWebApplicationFactory factory) => _factory = factory;

    [Fact(Timeout = 120000)]
    public async Task GET_support_diagnostics_returns_redacted_bundle()
    {
        var client = _factory.CreateClient();
        using var response = await client.GetAsync("api/support/diagnostics");
        response.EnsureSuccessStatusCode();

        var diagnostics = await response.Content.ReadFromJsonAsync<SupportDiagnosticsResponse>();
        diagnostics.Should().NotBeNull();
        diagnostics!.Application.Should().Be("Ashlar.API");
        diagnostics.Security.ApiKeyConfigured.Should().BeFalse();
        diagnostics.License.State.Should().Be(nameof(PrivateLicenseState.NotConfigured));
        diagnostics.RedactedConfiguration.Should().NotBeEmpty();
        diagnostics.RedactedConfiguration.Values.Should().NotContain("secret-key");
    }
}
