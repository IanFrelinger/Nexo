using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Infrastructure.Autonomy;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Autonomy;

/// <summary>
/// The periodic human digest (autonomy spec §7) as a host job: swap provenance retained in
/// a bounded ring, rendered on cadence, honest about saturation — and composed so that
/// whichever order a host calls <c>AddNexoAutonomy</c> and <c>AddNexoAutonomyDigestJob</c>,
/// the retaining sink is the one serving.
/// </summary>
[Trait("Category", "Certification")]
public sealed class AutonomyDigestJobTests
{
    [Fact]
    public void RecordingSink_RetainsBounded_AndCountsWhatFell()
    {
        var sink = new RecordingBrickSwapProvenanceSink(logger: null, capacity: 2);

        for (var i = 1; i <= 3; i++)
            sink.Record(Event(i));

        sink.Snapshot().Select(e => e.Generation).Should().Equal(new[] { 2, 3 },
            "the ring keeps the most recent events");
        sink.DroppedEvents.Should().Be(1,
            "a digest over a saturated ring must be able to say so instead of presenting "
            + "a truncated history as complete");
    }

    [Fact]
    public void RenderOnce_RendersTheDigest_AndNamesSaturation()
    {
        var sink = new RecordingBrickSwapProvenanceSink(logger: null, capacity: 1);
        var service = Service(sink);

        sink.Record(Event(1, BrickSwapProvenanceOutcomes.SwapCommitted));
        service.RenderOnce().Should().Contain("swap-committed").And.NotContain("ring saturated");

        sink.Record(Event(2, BrickSwapProvenanceOutcomes.SwapCommitted));
        service.RenderOnce().Should().Contain("ring saturated",
            "once events have fallen off, the digest says it shows a window, not the world");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DigestJob_TakesOverTheSinkBinding_InEitherCompositionOrder(bool digestFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCertificationGate();
        if (digestFirst)
        {
            services.AddNexoAutonomyDigestJob();
            services.AddNexoAutonomy();
        }
        else
        {
            services.AddNexoAutonomy();
            services.AddNexoAutonomyDigestJob();
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        provider.GetRequiredService<ICertifiedBrickSwapProvenanceSink>()
            .Should().BeSameAs(provider.GetRequiredService<RecordingBrickSwapProvenanceSink>(),
                "the retaining sink must serve regardless of registration order — otherwise "
                + "the digest renders an empty history while the loop swaps away");
        provider.GetServices<IHostedService>().Should().Contain(s => s is AutonomyDigestService);
    }

    [Fact]
    public async Task DigestJob_WithIntervalZero_DeclinesQuietly()
    {
        var service = Service(new RecordingBrickSwapProvenanceSink(), digestIntervalSeconds: 0);

        // StartAsync runs ExecuteAsync; with the job disabled it must complete promptly
        // rather than parking a timer nobody configured.
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    private static AutonomyDigestService Service(
        RecordingBrickSwapProvenanceSink sink, int digestIntervalSeconds = 3600) => new(
        sink,
        Options.Create(new NexoAutonomyOptions { DigestIntervalSeconds = digestIntervalSeconds }),
        NullLogger<AutonomyDigestService>.Instance);

    private static BrickSwapProvenanceEvent Event(
        int generation, string outcome = BrickSwapProvenanceOutcomes.SwapCommitted) => new()
    {
        Generation = generation,
        Outcome = outcome,
        Timestamp = DateTimeOffset.UtcNow,
        BrickId = "digest-probe",
    };
}
