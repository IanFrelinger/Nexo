using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public sealed class DataDecisionAuditLogContractTests : Nexo.Tests.Contracts.DataDecisionAuditLogContractTests
{
    protected override IDataDecisionAuditLog CreateInstance() => new DataDecisionAuditLog();
}
