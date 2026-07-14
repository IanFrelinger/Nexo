using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Nexo.AI.Pipeline.Governance;

namespace Nexo.AI.Pipeline.Embeddings;

/// <summary>
/// Sanitizes outbound embedding inputs (queries and documents) before generation.
/// </summary>
public sealed class SanitizingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IChatMessageSanitizer _sanitizer;
    private readonly SanitizeDisposition _disposition;

    /// <summary>Creates a sanitizing embedding decorator.</summary>
    public SanitizingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> inner,
        IChatMessageSanitizer sanitizer,
        SanitizeDisposition disposition = SanitizeDisposition.Redact)
        : base(inner)
    {
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _disposition = disposition;
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sanitized = new List<string>();
        foreach (var value in values)
        {
            var messages = new List<ChatMessage> { new(ChatRole.User, value ?? string.Empty) };
            var result = _sanitizer.Sanitize(messages, targetKey: "embed:local", _disposition, cancellationToken);
            if (!result.Allowed)
            {
                throw new PolicyViolationException(
                    code: "sanitization_blocked",
                    message: result.BlockReason ?? "Embedding input blocked by sanitization policy.",
                    targetKey: "embed:local");
            }

            sanitized.Add(result.Messages![0].Text ?? string.Empty);
            // Set before awaiting the audit/provider layer so the inner auditor can read it.
            SanitizationCallContext.Result = result;
        }

        return await base.GenerateAsync(sanitized, options, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Audits embedding generation calls (counts only; never logs content).
/// </summary>
public sealed class AuditingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IChatInvocationAuditor _auditor;

    /// <summary>Creates an auditing embedding decorator.</summary>
    public AuditingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> inner,
        IChatInvocationAuditor auditor)
        : base(inner)
    {
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialised = values.ToList();
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await base.GenerateAsync(materialised, options, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            var sanitize = SanitizationCallContext.Result;
            _auditor.Record(new ChatInvocationAuditRecord
            {
                TargetKey = "embed:local",
                Outcome = "success",
                PolicyDecisions = new[] { "event=embedding", $"inputs={materialised.Count}" },
                RedactionCount = sanitize?.RedactionCount ?? 0,
                RedactionCategories = sanitize?.Categories ?? Array.Empty<string>(),
                LatencyMs = sw.ElapsedMilliseconds,
            });
            return result;
        }
        catch (Exception)
        {
            sw.Stop();
            _auditor.Record(new ChatInvocationAuditRecord
            {
                TargetKey = "embed:local",
                Outcome = "fault",
                PolicyDecisions = new[] { "event=embedding" },
                ReasonCode = "fault",
                LatencyMs = sw.ElapsedMilliseconds,
            });
            throw;
        }
    }
}
