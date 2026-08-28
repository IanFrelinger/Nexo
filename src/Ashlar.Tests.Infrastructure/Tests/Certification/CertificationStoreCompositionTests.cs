using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Sdk.Extensions;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Composition order must not decide whether admissions survive the process.
///
/// <para>The regression these pin: <c>AddCertificationGate()</c> took no store path and
/// forwarded none, and the in-memory store was registered with <c>AddSingleton</c> rather
/// than <c>TryAddSingleton</c>. Because last-registration-wins for <c>GetRequiredService</c>,
/// a host that asked for durability and then added the gate had its durable store replaced
/// with an in-memory one — no exception, no log line. For the CLI, where every invocation is
/// a fresh process, that means nothing certified could ever be admitted again.</para>
///
/// <para>The rule being fixed in place is <b>explicit beats default, in either order</b>.
/// Simply switching to TryAdd would have satisfied the first order and broken the second,
/// moving the silent failure rather than removing it, so both directions are pinned.</para>
///
/// <para>These return <c>Task</c> rather than <c>void</c> because the ProdStyle trait enrols
/// them in the timeout convention (<c>TimeoutConventionTests</c>), and xunit enforces
/// <c>Timeout</c> by racing the returned task — it rejects a void test at run time. The
/// timeout is a hang net, not a budget: these run in milliseconds.</para>
/// </summary>
[Trait("Category", "Certification")]
[Trait("Category", "ProdStyle")]
public sealed class CertificationStoreCompositionTests : TempDirTestBase
{
    public CertificationStoreCompositionTests()
        : base("ashlar-cert-store-composition")
    {
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public Task DurableStore_ThenGate_KeepsTheDurableStore()
    {
        var services = new ServiceCollection();
        services.AddCertificationInfrastructure(recordStorePath: TempDir);
        services.AddCertificationGate();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificationRecordStore>()
            .Should().BeOfType<FileCertificationRecordStore>(
                "a gate added after a durable store must not silently revert it to in-memory");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public Task Gate_ThenDurableStore_AlsoKeepsTheDurableStore()
    {
        var services = new ServiceCollection();
        services.AddCertificationGate();
        services.AddCertificationInfrastructure(recordStorePath: TempDir);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificationRecordStore>()
            .Should().BeOfType<FileCertificationRecordStore>(
                "an explicit store path is a decision and must win over a default already registered");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public Task Gate_CanComposeDurablyOnItsOwn()
    {
        var services = new ServiceCollection();
        services.AddCertificationGate(recordStorePath: TempDir);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificationRecordStore>()
            .Should().BeOfType<FileCertificationRecordStore>(
                "the gate is the composition root hosts actually call, so it must be able to ask for durability");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public Task Gate_WithoutAPath_StillDefaultsToInMemory()
    {
        var services = new ServiceCollection();
        services.AddCertificationGate();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificationRecordStore>()
            .Should().BeOfType<InMemoryCertificationRecordStore>(
                "the default is unchanged; only its ability to displace an explicit choice was removed");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public Task AHostSuppliedSigner_SurvivesComposition()
    {
        // Supplying a signer is the only way to hold a real HMAC key (SPEC-006 S-4), so
        // overwriting it with a parameterless one would silently drop the host back to the
        // committed dev key — the same class of failure as the store, on the key path.
        var configured = new CertificationRecordSigner(hmacKey: "not-the-committed-dev-key");

        var services = new ServiceCollection();
        services.AddSingleton(configured);
        services.AddCertificationGate(recordStorePath: TempDir);

        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<CertificationRecordSigner>();
        resolved.Should().BeSameAs(configured);
        resolved.UsesDevKey.Should().BeFalse(
            "a host that configured a real key must not be silently reverted to the forgeable one");

        return Task.CompletedTask;
    }
}
