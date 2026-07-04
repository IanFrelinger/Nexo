using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

/// <summary>Tests for data decision audit log contract.</summary>
public sealed class DataDecisionAuditLogContractTests : Nexo.Tests.Contracts.DataDecisionAuditLogContractTests
{
    /// <summary>Creates instance.</summary>
    protected override IDataDecisionAuditLog CreateInstance() => new DataDecisionAuditLog();
}
