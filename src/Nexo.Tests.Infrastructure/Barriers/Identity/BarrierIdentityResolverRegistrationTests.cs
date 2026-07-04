using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for barrier identity resolver registration.</summary>
public sealed class BarrierIdentityResolverRegistrationTests
{
    [Fact]
    public void AddBarrierIdentityResolvers_UnknownResolver_ThrowsAtStartup()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Identity:ResolverPriority:0"] = "Nope"
        });
        var services = new ServiceCollection();

        var act = () => services.AddBarrierIdentityResolvers(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown barrier identity resolver*");
    }

    [Fact]
    public void AddBarrierIdentityResolvers_RegistersResolversInPriorityOrder()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Barriers:Levels:1"] = "internal",
            ["Nexo:Identity:ResolverPriority:0"] = "JwtClaim",
            ["Nexo:Identity:ResolverPriority:1"] = "ApiKey"
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
            return new BarrierHierarchy(options.Levels.Select((name, i) => new BarrierLevel(name, i)));
        });

        services.AddBarrierIdentityResolvers(configuration);

        using var provider = services.BuildServiceProvider();
        var resolvers = provider.GetServices<IBarrierIdentityResolver>().ToList();
        resolvers.Should().HaveCount(2);
        resolvers[0].Should().BeOfType<JwtClaimBarrierResolver>();
        resolvers[1].Should().BeOfType<ApiKeyBarrierResolver>();
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
