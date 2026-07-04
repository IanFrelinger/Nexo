using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for immutable core registry contract tests impl.</summary>
public sealed class ImmutableCoreRegistryContractTestsImpl : ImmutableCoreRegistryContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IImmutableCoreRegistry CreateInstance() => new ImmutableCoreRegistry();
}
