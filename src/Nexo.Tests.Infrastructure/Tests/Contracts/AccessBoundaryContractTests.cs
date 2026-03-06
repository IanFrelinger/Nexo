using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

public sealed class AccessBoundaryContractTests : Nexo.Tests.Contracts.AccessBoundaryContractTests
{
    protected override IAccessBoundary CreateInstance() => new AccessBoundary();
}
