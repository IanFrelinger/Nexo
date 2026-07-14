using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Nexo.AI.Pipeline.Governance;

/// <summary>
/// Sanitizes outbound messages before delegation. Streaming responses pass through untouched.
/// </summary>
public sealed class SanitizingChatClient : DelegatingChatClient
{
    private readonly IChatMessageSanitizer _sanitizer;
    private readonly ITargetSanitizePolicy _policy;
    private readonly IChatInvocationAuditor _auditor;
    private readonly string _targetKey;

    /// <summary>Creates a sanitizing decorator.</summary>
    public SanitizingChatClient(
        IChatClient innerClient,
        IChatMessageSanitizer sanitizer,
        ITargetSanitizePolicy policy,
        IChatInvocationAuditor auditor,
        string targetKey)
        : base(innerClient)
    {
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
        _targetKey = targetKey ?? throw new ArgumentNullException(nameof(targetKey));
    }

    /// <summary>Last sanitization summary for the current call path (for auditing).</summary>
    public MessageSanitizationResult? LastResult { get; private set; }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sanitized = SanitizeOrThrow(messages, cancellationToken);
        return await base.GetResponseAsync(sanitized, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sanitized = SanitizeOrThrow(messages, cancellationToken);
        await foreach (var update in base.GetStreamingResponseAsync(sanitized, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private IList<ChatMessage> SanitizeOrThrow(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        var disposition = _policy.GetDisposition(_targetKey);
        var result = _sanitizer.Sanitize(list, _targetKey, disposition, cancellationToken);
        LastResult = result;
        SanitizationCallContext.Result = result;

        if (!result.Allowed)
        {
            _auditor.Record(new ChatInvocationAuditRecord
            {
                TargetKey = _targetKey,
                Outcome = "denied",
                PolicyDecisions = new[] { $"target={_targetKey}", "decision=sanitize_block" },
                RedactionCount = 0,
                RedactionCategories = result.Categories,
                ReasonCode = "sanitization_blocked",
            });

            throw new PolicyViolationException(
                code: "sanitization_blocked",
                message: result.BlockReason ?? "Request blocked by sanitization policy.",
                targetKey: _targetKey,
                details: result.Categories.Count == 0
                    ? null
                    : new Dictionary<string, string> { ["categories"] = string.Join(',', result.Categories) });
        }

        return result.Messages!;
    }
}
