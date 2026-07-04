using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;
using Nexo.Tests.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for access boundary contract.</summary>
public sealed class AccessBoundaryContractTests : Nexo.Tests.Contracts.AccessBoundaryContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IAccessBoundary CreateInstance() => new AccessBoundary();
}
