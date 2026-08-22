using Ashlar.BackgroundAgents.Trust;
using Ashlar.Core.Application.Trust.Ports;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Trust;

/// <summary>Tests for lite db data decision audit log contract.</summary>
public sealed class LiteDbDataDecisionAuditLogContractTests : Ashlar.Tests.Contracts.DataDecisionAuditLogContractTests
{
    protected override IDataDecisionAuditLog CreateInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ashlar-litedb-contract-{Guid.NewGuid():N}.db");
        /// <summary>Lite db data decision audit log.</summary>
        return new LiteDbDataDecisionAuditLog(dbPath);
    }
}
