using Ashlar.BackgroundAgents.Trust;
using Ashlar.Core.Application.Trust.Ports;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Trust;

/// <summary>Tests for data decision audit log contract.</summary>
public sealed class DataDecisionAuditLogContractTests : Ashlar.Tests.Contracts.DataDecisionAuditLogContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IDataDecisionAuditLog CreateInstance() => new DataDecisionAuditLog();
}
