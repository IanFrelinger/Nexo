using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.AI.Pipeline;
using Nexo.Tests.Infrastructure.Helpers;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage for the MEAI Ollama fields on <c>GET /api/onboarding/status</c>: the endpoint
/// must report the base URL and model the default model path will actually dial, resolved through
/// the same precedence as the client. This is what <c>scripts/prod-dry-run.sh</c> reads to fail a
/// container that would dial its own loopback while "Ollama available" says otherwise (H3).
/// Runs the production Program with the full middleware chain.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class OnboardingStatusMeaiOllamaProdStyleTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        IDictionary<string, string?>? settings = null,
        Action<IServiceCollection>? configureServices = null)
        => new NexoApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (settings is not null)
            {
                foreach (var pair in settings)
                {
                    builder.UseSetting(pair.Key, pair.Value);
                }
            }

            if (configureServices is not null)
            {
                // Later registrations win for plain resolutions.
                builder.ConfigureServices(configureServices);
            }
        });

    private static async Task<JsonElement> GetOnboardingStatusAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/onboarding/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Onboarding_status_reports_the_meai_ollama_endpoint_the_default_model_path_dials()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var root = await GetOnboardingStatusAsync(client);

        // MEAI is default-on, so the fields must be populated (never null) with the resolved values.
        root.TryGetProperty("meaiOllamaBaseUrl", out var baseUrl).Should().BeTrue();
        baseUrl.ValueKind.Should().Be(JsonValueKind.String);
        baseUrl.GetString().Should().NotBeNullOrWhiteSpace();
        Uri.TryCreate(baseUrl.GetString(), UriKind.Absolute, out _).Should().BeTrue();

        root.TryGetProperty("meaiOllamaModel", out var model).Should().BeTrue();
        model.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Onboarding_status_reflects_the_bound_Meai_options()
    {
        // The compose stacks set Nexo__Meai__OllamaBaseUrl/OllamaModel so a container dials the
        // ollama service, not its own loopback; the status endpoint must report exactly what the
        // pipeline was bound with. The options are overridden in DI rather than via UseSetting
        // because AddNexo(options => ...) builds its own IConfiguration from environment
        // variables only (NexoServiceCollectionExtensions.AddNexo) - host configuration such as
        // appsettings.json never reaches Nexo:Meai. That gap is a separate finding; this test
        // pins the endpoint's contract, which is "report the bound options".
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Nexo:Meai:OllamaBaseUrl"] = "http://ollama:11434",
            ["Nexo:Meai:OllamaModel"] = "qwen2.5-coder:7b",
        }, services => services.AddSingleton<IOptions<MeaiPipelineOptions>>(
            Options.Create(new MeaiPipelineOptions
            {
                OllamaBaseUrl = "http://ollama:11434",
                OllamaModel = "qwen2.5-coder:7b",
            })));
        using var client = factory.CreateClient();

        var root = await GetOnboardingStatusAsync(client);

        root.GetProperty("meaiOllamaBaseUrl").GetString().Should().Be("http://ollama:11434");
        root.GetProperty("meaiOllamaModel").GetString().Should().Be("qwen2.5-coder:7b");
    }
}
