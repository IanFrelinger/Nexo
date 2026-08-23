using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Runtime;

/// <summary>Tests for runtime service collection extensions.</summary>
public sealed class RuntimeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAshlarRuntimeRouting_NoBarrierLevelsConfigured_UsesSafeDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        services.AddAshlarRuntimeRouting(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BarrierOptions>>().Value;
        var hierarchy = provider.GetRequiredService<BarrierHierarchy>();

        options.Levels.Should().ContainInOrder("public", "internal");
        hierarchy.Floor.Name.Should().Be("public");
        hierarchy.Ceiling.Name.Should().Be("internal");
    }

    [Fact]
    public void AddAshlarRuntimeRouting_CustomBarrierLevelsConfigured_PreservesConfiguredValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ashlar:Barriers:Levels:0"] = "team",
            ["Ashlar:Barriers:Levels:1"] = "prod",
            ["Ashlar:Barriers:Levels:2"] = "secret"
        });
        services.AddAshlarRuntimeRouting(configuration);

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
