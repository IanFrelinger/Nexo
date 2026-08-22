using FluentAssertions;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for in memory mesh task registry.</summary>
public sealed class InMemoryMeshTaskRegistryTests
{
    [Fact]
    public async Task Create_with_same_idempotency_key_returns_same_task()
    {
        var reg = new InMemoryMeshTaskRegistry();
        var spec = new MeshTaskCreateSpec("x", 1, [], null, 0, null, null, "idem-1");
        var a = await reg.CreateAsync(spec);
        var b = await reg.CreateAsync(spec);
        a.TaskId.Should().Be(b.TaskId);
        (await reg.TryGetByIdempotencyKeyAsync("idem-1"))!.TaskId.Should().Be(a.TaskId);
    }
}
