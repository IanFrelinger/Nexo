using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

public sealed class ImmutableCoreRegistryContractTestsImpl : ImmutableCoreRegistryContractTests
{
    protected override IImmutableCoreRegistry CreateInstance() => new ImmutableCoreRegistry();
}
