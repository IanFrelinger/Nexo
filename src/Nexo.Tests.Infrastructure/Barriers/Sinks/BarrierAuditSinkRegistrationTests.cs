using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

public sealed class BarrierAuditSinkRegistrationTests
{
    [Fact]
    public void AddBarrierAuditSinks_UnknownSink_ThrowsAtStartup()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "Flie"
        });
        var services = new ServiceCollection();

        var act = () => services.AddBarrierAuditSinks(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown audit sink*");
    }

    [Fact]
    public void AddBarrierAuditSinks_EmptySinkList_RegistersNoOpSink()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBarrierAuditSinks(configuration);

        using var provider = services.BuildServiceProvider();
        var sinks = provider.GetServices<IBarrierAuditSink>().ToList();
        sinks.Should().ContainSingle();
        sinks[0].Should().BeOfType<NoOpBarrierAuditSink>();
    }

    [Fact]
    public async Task AddBarrierAuditSinks_FileAndStructuredLog_RegistersBoth()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-audit-reg-tests", Guid.NewGuid().ToString("N"));
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "File",
            ["Nexo:Audit:Sinks:1"] = "StructuredLog",
            ["Nexo:Audit:File:Directory"] = tempDir
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        await using var provider = services.BuildServiceProvider();
        var sinks = provider.GetServices<IBarrierAuditSink>().ToList();
        sinks.Should().Contain(sink => sink is FileBarrierAuditSink);
        sinks.Should().Contain(sink => sink is StructuredLogBarrierAuditSink);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
