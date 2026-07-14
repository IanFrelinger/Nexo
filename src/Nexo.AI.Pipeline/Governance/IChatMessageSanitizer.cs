using Microsoft.Extensions.AI;

namespace Nexo.AI.Pipeline.Governance;

/// <summary>
/// Result of sanitizing outbound chat messages.
/// </summary>
public sealed class MessageSanitizationResult
{
    private MessageSanitizationResult(
        bool allowed,
        IList<ChatMessage>? messages,
        string? blockReason,
        int redactionCount,
        IReadOnlyList<string> categories)
    {
        Allowed = allowed;
        Messages = messages;
        BlockReason = blockReason;
        RedactionCount = redactionCount;
        Categories = categories;
    }

    /// <summary>Whether the call may proceed.</summary>
    public bool Allowed { get; }

    /// <summary>Sanitized messages when allowed.</summary>
    public IList<ChatMessage>? Messages { get; }

    /// <summary>Block reason when not allowed.</summary>
    public string? BlockReason { get; }

    /// <summary>Number of redactions applied (never includes content).</summary>
    public int RedactionCount { get; }

    /// <summary>Redaction/block categories (e.g. email, api-key).</summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>Creates an allowed result.</summary>
    public static MessageSanitizationResult Allow(
        IList<ChatMessage> messages,
        int redactionCount = 0,
        IReadOnlyList<string>? categories = null) =>
        new(true, messages, null, redactionCount, categories ?? Array.Empty<string>());

    /// <summary>Creates a blocked result.</summary>
    public static MessageSanitizationResult Block(string reason, IReadOnlyList<string>? categories = null) =>
        new(false, null, reason, 0, categories ?? Array.Empty<string>());
}

/// <summary>
/// Sanitizes outbound messages for a destination target.
/// </summary>
public interface IChatMessageSanitizer
{
    /// <summary>Sanitizes all outbound messages according to the target's disposition.</summary>
    MessageSanitizationResult Sanitize(
        IList<ChatMessage> messages,
        string targetKey,
        SanitizeDisposition disposition,
        CancellationToken cancellationToken = default);
}
