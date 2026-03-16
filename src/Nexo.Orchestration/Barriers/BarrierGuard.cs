using Nexo.Abstractions.Barriers;

namespace Nexo.Orchestration.Barriers;

internal sealed class BarrierGuard
{
    private readonly IBarrierContextAccessor _accessor;
    private readonly IBarrierAuditLog _auditLog;
    private readonly BarrierHierarchy _hierarchy;
    private readonly BarrierOptions _options;

    public BarrierGuard(
        IBarrierContextAccessor accessor,
        IBarrierAuditLog auditLog,
        BarrierHierarchy hierarchy,
        BarrierOptions options)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task AssertValidForInvocationAsync(
        string agentName,
        string correlationId,
        string spanId,
        CancellationToken cancellationToken)
    {
        var context = _accessor.Current;

        if (context is null)
        {
            if (_options.RequireExplicitBarrier)
                throw new BarrierContextMissingException(agentName, correlationId);

            context = BarrierContext.Create(
                _hierarchy.Floor.Name,
                BarrierAuthoritySource.Default,
                agentName,
                correlationId,
                _hierarchy);

            await _auditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.DefaultApplied,
                context.Level,
                context.AuthoritySource,
                agentName,
                correlationId,
                spanId,
                DateTimeOffset.UtcNow,
                "No explicit barrier set; defaulted to floor level."),
                cancellationToken);
        }

        await _auditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.AgentInvoked,
            context.Level,
            context.AuthoritySource,
            agentName,
            correlationId,
            spanId,
            DateTimeOffset.UtcNow,
            BuildResolutionDetail(context)),
            cancellationToken);
    }

    public void AssertNoElevation(
        string requestedLevel,
        string currentLevel,
        string agentName,
        string correlationId)
    {
        if (!_hierarchy.IsAbove(requestedLevel, currentLevel))
            return;

        _ = _auditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.ElevationDenied,
            requestedLevel,
            "Unknown",
            agentName,
            correlationId,
            string.Empty,
            DateTimeOffset.UtcNow,
            $"Attempted elevation from '{currentLevel}' to '{requestedLevel}'."));

        throw new BarrierElevationException(agentName, correlationId, currentLevel, requestedLevel);
    }

    private static string BuildResolutionDetail(BarrierContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.ResolutionDetail))
            return context.ResolutionDetail;

        return $"Resolved by: {context.AuthoritySource}.";
    }
}
