using FluentAssertions;
using Nexo.Runtime.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier identity resolver options gap coverage.</summary>
public sealed class BarrierIdentityResolverOptionsGapCoverageTests
{
    [Fact]
    public void ResolverPriority_starts_empty()
    {
        new BarrierIdentityResolverOptions().ResolverPriority.Should().BeEmpty();
    }
}
