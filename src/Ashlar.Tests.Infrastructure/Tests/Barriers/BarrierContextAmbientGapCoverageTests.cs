using FluentAssertions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime.Barriers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier context ambient gap coverage.</summary>
public sealed class BarrierContextAmbientGapCoverageTests
{
    [Fact]
    public void SetCurrent_and_Current_round_trip_context()
    {
        var ambient = new BarrierContextAmbient();
        var hierarchy = CreateHierarchy();
        var context = BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Header,
            issuedTo: string.Empty,
            correlationId: "corr-ambient",
            hierarchy,
            resolutionDetail: "test");

        ambient.SetCurrent(context);

        ambient.Current.Should().BeSameAs(context);
    }

    [Fact]
    public void SetCurrent_null_clears_ambient_context()
    {
        var ambient = new BarrierContextAmbient();
        var hierarchy = CreateHierarchy();
        ambient.SetCurrent(BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Header,
            issuedTo: string.Empty,
            correlationId: "corr-ambient",
            hierarchy));

        ambient.SetCurrent(null);

        ambient.Current.Should().BeNull();
    }

    [Fact]
    public async Task AsyncLocal_is_isolated_between_parallel_tasks()
    {
        var ambient = new BarrierContextAmbient();
        var hierarchy = CreateHierarchy();
        var publicContext = BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Header,
            issuedTo: string.Empty,
            correlationId: "public",
            hierarchy);
        var internalContext = BarrierContext.Create(
            "internal",
            BarrierAuthoritySource.JwtClaim,
            issuedTo: string.Empty,
            correlationId: "internal",
            hierarchy);

        var publicTask = Task.Run(() =>
        {
            ambient.SetCurrent(publicContext);
            Thread.Sleep(50);
            return ambient.Current?.Level;
        });
        var internalTask = Task.Run(() =>
        {
            ambient.SetCurrent(internalContext);
            Thread.Sleep(50);
            return ambient.Current?.Level;
        });

        var levels = await Task.WhenAll(publicTask, internalTask);

        levels.Should().BeEquivalentTo(["public", "internal"]);
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
        ]);
}
