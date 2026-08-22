using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime.Barriers;
using Xunit;

namespace Ashlar.Tests.Orchestration.Barriers;

/// <summary>Tests for scoped barrier context accessor.</summary>
public sealed class ScopedBarrierContextAccessorTests
{
    [Fact]
    public void Initialize_Once_CurrentReflectsContext()
    {
        var accessor = CreateAccessor(new BarrierOptions
        {
            Levels = ["low", "high"]
        });
        var hierarchy = CreateHierarchy("low", "high");
        var context = BarrierContext.Create("low", BarrierAuthoritySource.Cli, "*", "corr-1", hierarchy);

        accessor.Initialize(context);

        accessor.Current.Should().Be(context);
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        var accessor = CreateAccessor(new BarrierOptions
        {
            Levels = ["low", "high"]
        });
        var hierarchy = CreateHierarchy("low", "high");
        var context = BarrierContext.Create("low", BarrierAuthoritySource.Cli, "*", "corr-1", hierarchy);

        accessor.Initialize(context);
        var act = () => accessor.Initialize(context);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConcurrentInitialize_ExactlyOneSucceeds()
    {
        var accessor = CreateAccessor(new BarrierOptions
        {
            Levels = ["low", "high"]
        });
        var hierarchy = CreateHierarchy("low", "high");
        var context = BarrierContext.Create("low", BarrierAuthoritySource.Cli, "*", "corr-1", hierarchy);
        var successes = 0;
        var failures = new ConcurrentBag<Exception>();

        Parallel.For(0, 50, _ =>
        {
            try
            {
                accessor.Initialize(context);
                Interlocked.Increment(ref successes);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        });

        successes.Should().Be(1);
        failures.Should().NotBeEmpty();
        failures.Should().OnlyContain(ex => ex is InvalidOperationException);
    }

    [Fact]
    public void Initialize_LevelAboveHostCeiling_ThrowsBarrierCeilingExceeded()
    {
        var accessor = CreateAccessor(new BarrierOptions
        {
            Levels = ["public", "phi", "restricted"],
            HostCeiling = "phi"
        });
        var hierarchy = CreateHierarchy("public", "phi", "restricted");
        var context = BarrierContext.Create("restricted", BarrierAuthoritySource.Cli, "*", "corr-1", hierarchy);

        var act = () => accessor.Initialize(context);

        act.Should().Throw<BarrierCeilingExceededException>();
    }

    private static ScopedBarrierContextAccessor CreateAccessor(BarrierOptions options)
    {
        var hierarchy = new BarrierHierarchy(options.Levels.Select((name, rank) => new BarrierLevel(name, rank)));
        return new ScopedBarrierContextAccessor(hierarchy, Options.Create(options));
    }

    private static BarrierHierarchy CreateHierarchy(params string[] levels)
    {
        /// <summary>Barrier hierarchy.</summary>
        return new BarrierHierarchy(levels.Select((name, rank) => new BarrierLevel(name, rank)));
    }
}
