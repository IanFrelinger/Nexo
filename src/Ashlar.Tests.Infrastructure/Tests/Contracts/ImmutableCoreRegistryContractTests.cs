using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Tests.Contracts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for immutable core registry contract tests impl.</summary>
public sealed class ImmutableCoreRegistryContractTestsImpl : ImmutableCoreRegistryContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IImmutableCoreRegistry CreateInstance() => new ImmutableCoreRegistry();
}
