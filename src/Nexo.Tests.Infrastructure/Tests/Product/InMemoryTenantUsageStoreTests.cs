using FluentAssertions;
using Nexo.Infrastructure.Product;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Product;

/// <summary>Tests for in memory tenant usage store.</summary>
public sealed class InMemoryTenantUsageStoreTests
{
    [Fact]
    public void GetSummary_isolates_tenants_and_counts_jobs()
    {
        var store = new InMemoryTenantUsageStore();
        store.RecordJobSubmitted("tenant-a");
        store.RecordJobCompleted("tenant-a", success: true);
        store.RecordJobSubmitted("tenant-b");
        store.RecordJobCompleted("tenant-b", success: false);

        var a = store.GetSummary("tenant-a", windowHours: 24);
        var b = store.GetSummary("tenant-b", windowHours: 24);

        a.JobsSubmitted.Should().Be(1);
        a.JobsSucceeded.Should().Be(1);
        a.JobsFailed.Should().Be(0);

        b.JobsSubmitted.Should().Be(1);
        b.JobsSucceeded.Should().Be(0);
        b.JobsFailed.Should().Be(1);
    }
}
