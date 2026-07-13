using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Infrastructure.Skills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class InMemorySkillApprovalStoreTests
{
    [Fact]
    public async Task WaitForResolutionAsync_times_out()
    {
        var store = new InMemorySkillApprovalStore();
        var pending = store.RegisterPending(
            new SkillScriptApprovalKey("skill", "s.sh", PeerTrustTier.Trusted, "internal", "pack"),
            "desc");

        var status = await store.WaitForResolutionAsync(pending.RequestId, TimeSpan.FromMilliseconds(50));
        status.Should().Be(NexoSkillApprovalStatus.TimedOut);
    }

    [Fact]
    public async Task TryResolve_completes_waiter()
    {
        var store = new InMemorySkillApprovalStore();
        var pending = store.RegisterPending(
            new SkillScriptApprovalKey("skill", "s.sh", PeerTrustTier.Trusted, "internal", "pack"),
            "desc");

        var wait = store.WaitForResolutionAsync(pending.RequestId, TimeSpan.FromSeconds(2));
        store.TryResolve(pending.RequestId, NexoSkillApprovalStatus.Denied, out _).Should().BeTrue();
        (await wait).Should().Be(NexoSkillApprovalStatus.Denied);
    }
}
