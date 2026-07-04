using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

/// <summary>Tests for lite db data decision audit log contract.</summary>
public sealed class LiteDbDataDecisionAuditLogContractTests : Nexo.Tests.Contracts.DataDecisionAuditLogContractTests
{
    protected override IDataDecisionAuditLog CreateInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"nexo-litedb-contract-{Guid.NewGuid():N}.db");
        /// <summary>Lite db data decision audit log.</summary>
        return new LiteDbDataDecisionAuditLog(dbPath);
    }
}
