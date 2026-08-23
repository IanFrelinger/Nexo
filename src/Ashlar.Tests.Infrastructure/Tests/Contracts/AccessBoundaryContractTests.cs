using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Trust;
using Ashlar.Tests.Contracts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Contracts;

/// <summary>Tests for access boundary contract.</summary>
public sealed class AccessBoundaryContractTests : Ashlar.Tests.Contracts.AccessBoundaryContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IAccessBoundary CreateInstance() => new AccessBoundary();
}
