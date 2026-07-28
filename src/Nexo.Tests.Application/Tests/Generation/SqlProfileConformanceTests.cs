using Nexo.Agents.TestKit;
using Nexo.Bricks.SqlProfile;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Part (e): the SQL profile against the SHARED conformance suite. Every case here
/// is inherited — this file supplies only what is genuinely profile-specific.
/// </summary>
public sealed class SqlProfileConformanceTests
    : AgentProfileConformanceTests<SqlAgentProfileFactory>
{
    /// <inheritdoc />
    protected override string ExpectedTargetId => SqlAgentProfileFactory.TargetId;

    /// <inheritdoc />
    protected override GenerationRequest OfflineRequest(OfflineAgentEnvironment environment) =>
        new()
        {
            TargetId = SqlAgentProfileFactory.TargetId,
            // The SQL deterministic path takes a bare table identifier.
            Grounding = "customers"
        };
}
