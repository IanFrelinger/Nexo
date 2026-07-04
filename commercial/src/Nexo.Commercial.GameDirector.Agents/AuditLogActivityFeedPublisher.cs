using System.Text.Json;
using GameDirector.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Agents;

public sealed class AuditLogActivityFeedPublisher : IActivityFeedPublisher
{
    private readonly IDataDecisionAuditLog _auditLog;

    public AuditLogActivityFeedPublisher(IDataDecisionAuditLog auditLog) => _auditLog = auditLog;

    public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
    {
        _auditLog.LogClassification("game-director-activity", eventType, JsonSerializer.Serialize(new
        {
            source,
            summary,
            at = DateTimeOffset.UtcNow
        }));
        return Task.CompletedTask;
    }
}
