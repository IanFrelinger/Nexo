using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Runtime;

public sealed class RuntimeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNexoRuntimeRouting_NoBarrierLevelsConfigured_UsesSafeDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        services.AddNexoRuntimeRouting(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BarrierOptions>>().Value;
        var hierarchy = provider.GetRequiredService<BarrierHierarchy>();

        options.Levels.Should().ContainInOrder("public", "internal");
        hierarchy.Floor.Name.Should().Be("public");
        hierarchy.Ceiling.Name.Should().Be("internal");
    }

    [Fact]
    public void AddNexoRuntimeRouting_CustomBarrierLevelsConfigured_PreservesConfiguredValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "team",
            ["Nexo:Barriers:Levels:1"] = "prod",
            ["Nexo:Barriers:Levels:2"] = "secret"
        });
        services.AddNexoRuntimeRouting(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BarrierOptions>>().Value;
        var hierarchy = provider.GetRequiredService<BarrierHierarchy>();

        options.Levels.Should().ContainInOrder("team", "prod", "secret");
        hierarchy.Floor.Name.Should().Be("team");
        hierarchy.Ceiling.Name.Should().Be("secret");
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
