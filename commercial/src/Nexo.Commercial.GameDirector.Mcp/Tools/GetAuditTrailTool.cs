using System.Text.Json;
using GameDirector.Domain;
using GameDirector.Mcp;
using Nexo.Core.Application.Trust.Ports;

namespace GameDirector.Mcp.Tools;

/// <summary>Get audit trail tool.</summary>
public sealed class GetAuditTrailTool : IMcpTool
{
    private readonly IDataDecisionAuditLog _auditLog;

    /// <summary>Gets audit trail tool.</summary>
    /// <param name="auditLog">Audit log.</param>
    public GetAuditTrailTool(IDataDecisionAuditLog auditLog) => _auditLog = auditLog;

    public string Name => "get_audit_trail";
    public string Description => "Retrieve audit records for AI decisions within a time range.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new { from = new { type = "string", format = "date-time" } },
        new { to = new { type = "string", format = "date-time" } },
        new { tool = new { type = "string" } },
        new { asset_id = new { type = "string" } });

    public Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var from = ParseDate(arguments, "from", DateTimeOffset.UtcNow.AddDays(-30));
        var to = ParseDate(arguments, "to", DateTimeOffset.UtcNow);
        var tool = arguments.TryGetProperty("tool", out var t) ? t.GetString() : null;
        var assetId = arguments.TryGetProperty("asset_id", out var a) ? a.GetString() : null;

        var records = AuditRecordParser.Query(_auditLog, from, to, tool, assetId);
        var payload = new
        {
            records = records.Select(r => new
            {
                audit_id = r.AuditId,
                timestamp = r.Timestamp,
                tool = r.Tool,
                input_hash = r.InputHash,
                model_id = r.ModelId,
                trust_policy = r.TrustPolicy,
                output_summary = r.OutputSummary,
                sanitization_events = r.SanitizationEvents
            }),
            total = records.Count
        };
        return Task.FromResult(JsonSerializer.SerializeToElement(payload, McpToolHelpers.JsonOptions));
    }

    /// <summary>Parse date.</summary>
    /// <param name="args">Args.</param>
    /// <param name="name">Name.</param>
    /// <param name="fallback">Fallback.</param>
    private static DateTimeOffset ParseDate(JsonElement args, string name, DateTimeOffset fallback) =>
        args.TryGetProperty(name, out var el) && DateTimeOffset.TryParse(el.GetString(), out var dt)
            ? dt
            : fallback;
}
