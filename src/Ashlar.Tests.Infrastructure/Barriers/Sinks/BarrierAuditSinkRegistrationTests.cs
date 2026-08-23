using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime;
using Ashlar.Runtime.Barriers.Sinks;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Tests for barrier audit sink registration.</summary>
public sealed class BarrierAuditSinkRegistrationTests
{
    [Fact]
    public void AddBarrierAuditSinks_UnknownSink_ThrowsAtStartup()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ashlar:Audit:Sinks:0"] = "Flie"
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
        var tempDir = Path.Combine(Path.GetTempPath(), "ashlar-audit-reg-tests", Guid.NewGuid().ToString("N"));
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ashlar:Audit:Sinks:0"] = "File",
            ["Ashlar:Audit:Sinks:1"] = "StructuredLog",
            ["Ashlar:Audit:File:Directory"] = tempDir
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        await using var provider = services.BuildServiceProvider();
        var sinks = provider.GetServices<IBarrierAuditSink>().ToList();
        sinks.Should().Contain(sink => sink is FileBarrierAuditSink);
        sinks.Should().Contain(sink => sink is StructuredLogBarrierAuditSink);
    }

    [Fact]
    public async Task AddBarrierAuditSinks_FileSink_RegistersHostedLifetime()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ashlar-audit-reg-tests", Guid.NewGuid().ToString("N"));
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ashlar:Audit:Sinks:0"] = "File",
            ["Ashlar:Audit:File:Directory"] = tempDir,
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        await using var provider = services.BuildServiceProvider();
        var lifetime = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<FileBarrierAuditSinkLifetime>()
            .Should()
            .ContainSingle()
            .Subject;

        await lifetime.StartAsync(CancellationToken.None);
        await lifetime.StopAsync(CancellationToken.None);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
