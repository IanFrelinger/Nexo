using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for copilot submission quota.</summary>
public sealed class CopilotSubmissionQuotaTests
{
    [Fact]
    public void TryConsume_enforces_quota_per_tenant_independently()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var quota = new CopilotSubmissionQuota(
            cache,
            Options.Create(new AshlarEntitlementsOptions { MaxCopilotSubmissionsPerHour = 1 }));

        quota.TryConsume("tenant-a", out _).Should().BeTrue();
        quota.TryConsume("tenant-a", out var deniedA).Should().BeFalse();
        deniedA.Should().Contain("tenant-a");

        quota.TryConsume("tenant-b", out _).Should().BeTrue();
    }
}
