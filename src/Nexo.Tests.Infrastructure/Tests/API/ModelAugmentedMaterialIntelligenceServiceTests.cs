using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions;
using Nexo.API.Forge;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Materials;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class ModelAugmentedMaterialIntelligenceServiceTests
{
    private sealed class StubModel : IModel
    {
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct) =>
            Task.FromResult(new ModelOutput("Prefer subtle roughness on terrain."));
    }

    [Fact]
    public async Task SuggestAsync_ModelDisabled_ReturnsBaseHintsOnly()
    {
        await using var sp = BuildProvider(enableMaterialModel: false);
        var svc = sp.GetRequiredService<IMaterialIntelligenceService>();
        var pack = BuiltInAestheticPacks.Catalog[0];

        var r = await svc.SuggestAsync(pack, "mvt");

        r.Summary.Should().NotContain("model:");
        r.Hints.Should().NotContain(h => h.Role == "model_notes");
    }

    [Fact]
    public async Task SuggestAsync_ModelEnabled_AppendsNoteHint()
    {
        await using var sp = BuildProvider(enableMaterialModel: true);
        var svc = sp.GetRequiredService<IMaterialIntelligenceService>();
        var pack = BuiltInAestheticPacks.Catalog[0];

        var r = await svc.SuggestAsync(pack, "mvt");

        r.Summary.Should().Contain("model:");
        r.Hints.Should().Contain(h => h.Role == "model_notes" && h.ColorOrParameterHint.Contains("subtle"));
    }

    private static ServiceProvider BuildProvider(bool enableMaterialModel)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.EnableMaterialModel = enableMaterialModel;
            o.MaterialModelTimeoutMs = 5000;
            o.MaxMaterialModelPromptChars = 8000;
        });
        services.AddSingleton<IModel, StubModel>();
        services.AddSingleton<HeuristicMaterialIntelligenceService>();
        services.AddSingleton<IMaterialIntelligenceService>(sp =>
            new ModelAugmentedMaterialIntelligenceService(
                sp.GetRequiredService<IModel>(),
                sp.GetRequiredService<HeuristicMaterialIntelligenceService>(),
                sp.GetRequiredService<IOptions<ForgeSessionOptions>>(),
                sp.GetRequiredService<ILogger<ModelAugmentedMaterialIntelligenceService>>()));

        return services.BuildServiceProvider();
    }
}
