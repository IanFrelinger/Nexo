using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace GameDirector.Domain;

/// <summary>
/// Records Game Director tool/brick decisions into the Nexo adaptation audit store.
/// </summary>
public static class GameDirectorAudit
{
    public static string NewAuditId() => Guid.NewGuid().ToString("N");

    public static string HashInput(object input)
    {
        var json = JsonSerializer.Serialize(input);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    public static void LogToolExecution(
        IDataDecisionAuditLog auditLog,
        string auditId,
        string tool,
        string inputHash,
        string modelId,
        string trustPolicy,
        string outputSummary,
        IReadOnlyList<string>? sanitizationEvents = null)
    {
        var reason = JsonSerializer.Serialize(new
        {
            audit_id = auditId,
            tool,
            input_hash = inputHash,
            model_id = modelId,
            trust_policy = trustPolicy,
            output_summary = outputSummary,
            sanitization_events = sanitizationEvents ?? []
        });

        auditLog.LogClassification("game-director", tool, reason);
    }
}
