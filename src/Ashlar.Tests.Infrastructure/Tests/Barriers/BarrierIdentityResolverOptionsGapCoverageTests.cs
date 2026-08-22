using FluentAssertions;
using Ashlar.Runtime.Barriers.Identity;
using Ashlar.Runtime.Barriers.Identity.Resolvers;
using Ashlar.Runtime.Barriers.Sinks;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier identity resolver options gap coverage.</summary>
public sealed class BarrierIdentityResolverOptionsGapCoverageTests
{
    [Fact]
    public void ResolverPriority_starts_empty()
    {
        new BarrierIdentityResolverOptions().ResolverPriority.Should().BeEmpty();
    }
}
