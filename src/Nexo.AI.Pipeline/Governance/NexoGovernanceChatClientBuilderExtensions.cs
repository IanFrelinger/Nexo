using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.AI.Pipeline.Governance;

/// <summary>
/// Fixed-order governance composition for Nexo chat clients.
/// </summary>
public static class NexoGovernanceChatClientBuilderExtensions
{
    /// <summary>
    /// Applies PolicyGate → Sanitizing → Auditing around the inner provider client.
    /// Hosts must use this extension so middleware cannot be mis-ordered.
    /// </summary>
    public static ChatClientBuilder UseNexoGovernance(this ChatClientBuilder builder, string targetKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetKey);

        // First Use = outermost → PolicyGate runs first.
        builder.Use((inner, sp) =>
        {
            var policy = sp.GetRequiredService<IChatTargetAccessPolicy>();
            var auditor = sp.GetRequiredService<IChatInvocationAuditor>();
            return new PolicyGateChatClient(inner, policy, auditor, targetKey);
        });

        builder.Use((inner, sp) =>
        {
            var sanitizer = sp.GetRequiredService<IChatMessageSanitizer>();
            var sanitizePolicy = sp.GetRequiredService<ITargetSanitizePolicy>();
            var auditor = sp.GetRequiredService<IChatInvocationAuditor>();
            return new SanitizingChatClient(inner, sanitizer, sanitizePolicy, auditor, targetKey);
        });

        builder.Use((inner, sp) =>
        {
            var auditor = sp.GetRequiredService<IChatInvocationAuditor>();
            return new AuditingChatClient(inner, auditor, targetKey);
        });

        return builder;
    }
}
