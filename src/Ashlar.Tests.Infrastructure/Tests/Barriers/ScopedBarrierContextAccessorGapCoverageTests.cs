using FluentAssertions;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Barriers;
using Ashlar.Runtime.Barriers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for scoped barrier context accessor gap coverage.</summary>
public sealed class ScopedBarrierContextAccessorGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var hierarchy = CreateHierarchy();
        var options = Options.Create(new BarrierOptions());

        var act = () => new ScopedBarrierContextAccessor(null!, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");

        act = () => new ScopedBarrierContextAccessor(hierarchy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Initialize_throws_for_null_context()
    {
        var accessor = CreateAccessor();

        var act = () => accessor.Initialize(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Initialize_sets_ambient_when_provided()
    {
        var ambient = new CapturingAmbient();
        var accessor = CreateAccessor(ambient: ambient);
        var context = CreateContext("public");

        accessor.Initialize(context);

        ambient.Current.Should().Be(context);
    }

    [Fact]
    public void Initialize_skips_ceiling_check_when_host_ceiling_is_blank()
    {
        var accessor = CreateAccessor(hostCeiling: "   ");
        var context = CreateContext("confidential");

        var act = () => accessor.Initialize(context);

        act.Should().NotThrow();
        accessor.Current.Should().Be(context);
    }

    [Fact]
    public void Initialize_allows_level_at_host_ceiling()
    {
        var accessor = CreateAccessor(hostCeiling: "internal");
        var context = CreateContext("internal");

        accessor.Initialize(context);

        accessor.Current!.Level.Should().Be("internal");
    }

    [Fact]
    public void Initialize_throws_when_level_exceeds_host_ceiling()
    {
        var accessor = CreateAccessor(hostCeiling: "public");
        var context = CreateContext("internal");

        var act = () => accessor.Initialize(context);

        act.Should().Throw<BarrierCeilingExceededException>();
    }

    [Fact]
    public void Initialize_throws_when_called_twice()
    {
        var accessor = CreateAccessor();
        accessor.Initialize(CreateContext("public"));

        var act = () => accessor.Initialize(CreateContext("internal"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been initialized*");
    }

    private static ScopedBarrierContextAccessor CreateAccessor(
        string? hostCeiling = null,
        IBarrierContextAmbient? ambient = null)
    {
        var hierarchy = CreateHierarchy();
        return new ScopedBarrierContextAccessor(
            hierarchy,
            Options.Create(new BarrierOptions { HostCeiling = hostCeiling }),
            ambient);
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);

    private static BarrierContext CreateContext(string level)
        => BarrierContext.Create(
            level,
            BarrierAuthoritySource.Header,
            issuedTo: string.Empty,
            correlationId: "corr-gap",
            CreateHierarchy(),
            resolutionDetail: "test");

    /// <summary>Tests for capturing ambient.</summary>
    private sealed class CapturingAmbient : IBarrierContextAmbient
    {
        /// <summary>Current.</summary>
        public BarrierContext? Current { get; private set; }

        /// <summary>Sets current.</summary>
        /// <param name="context">Context.</param>
        public void SetCurrent(BarrierContext? context) => Current = context;
    }
}
