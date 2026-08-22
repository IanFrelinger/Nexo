using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.WebSearch;

namespace Ashlar.BackgroundAgents.Trust;

/// <summary>
/// Default implementation of ICloudSanitizationProxy.
/// Uses ISensitiveContentFilter for PII and IDataTaxonomy for classification.
/// </summary>
public sealed class CloudSanitizationProxy : ICloudSanitizationProxy
{
    private const string RuleVersion = "trust-v1";
    private readonly ISensitiveContentFilter? _contentFilter;
    private readonly IDataTaxonomy? _taxonomy;
    private readonly ISanitizationAuditLog? _auditLog;

    /// <summary>
    /// Creates a new cloud sanitization proxy.
    /// </summary>
    /// <param name="contentFilter">Optional PII filter. If null, PII checks are skipped.</param>
    /// <param name="taxonomy">Optional taxonomy. If null, no data-type classification.</param>
    /// <param name="auditLog">Optional audit log. If null, redactions are not persisted.</param>
    public CloudSanitizationProxy(
        ISensitiveContentFilter? contentFilter = null,
        IDataTaxonomy? taxonomy = null,
        ISanitizationAuditLog? auditLog = null)
    {
        _contentFilter = contentFilter;
        _taxonomy = taxonomy;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public SanitizationResult SanitizeForCloud(OutgoingContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.IsAirGapped)
            return SanitizationResult.AllowedWith(context);

        var redactions = new List<SanitizationAuditEntry>();
        var systemPrompt = context.SystemPrompt ?? string.Empty;
        var userPrompt = context.UserPrompt ?? string.Empty;

        // PII check: block if query contains PII
        if (_contentFilter != null)
        {
            var combined = systemPrompt + "\n" + userPrompt;
            if (_contentFilter.ShouldBlockQuery(combined))
            {
                var entry = new SanitizationAuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    RuleVersion = RuleVersion,
                    FieldOrType = "prompt",
                    Disposition = "blocked",
                    Reason = "PII detected in prompt",
                };
                redactions.Add(entry);
                _auditLog?.LogRedaction(entry.Timestamp, entry.RuleVersion, entry.FieldOrType, entry.Disposition, entry.Reason);
                return SanitizationResult.Blocked("Prompt contains PII; blocked per policy.");
            }

            var filteredSystem = _contentFilter.FilterQuery(systemPrompt);
            var filteredUser = _contentFilter.FilterQuery(userPrompt);
            if (filteredSystem != systemPrompt || filteredUser != userPrompt)
            {
                var entry = new SanitizationAuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    RuleVersion = RuleVersion,
                    FieldOrType = "prompt",
                    Disposition = "redacted",
                    Reason = "PII redacted",
                };
                redactions.Add(entry);
                _auditLog?.LogRedaction(entry.Timestamp, entry.RuleVersion, entry.FieldOrType, entry.Disposition, entry.Reason);
                systemPrompt = filteredSystem;
                userPrompt = filteredUser;
            }
        }

        var sanitized = new OutgoingContext
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Variables = context.Variables,
            IsAirGapped = context.IsAirGapped,
            Provider = context.Provider,
        };

        foreach (var r in redactions)
            _auditLog?.LogRedaction(r.Timestamp, r.RuleVersion, r.FieldOrType, r.Disposition, r.Reason);

        return SanitizationResult.AllowedWith(sanitized, redactions);
    }
}
