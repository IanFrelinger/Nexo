using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

public sealed class LiteDbDataDecisionAuditLogContractTests : Nexo.Tests.Contracts.DataDecisionAuditLogContractTests
{
    protected override IDataDecisionAuditLog CreateInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"nexo-litedb-contract-{Guid.NewGuid():N}.db");
        return new LiteDbDataDecisionAuditLog(dbPath);
    }
}
