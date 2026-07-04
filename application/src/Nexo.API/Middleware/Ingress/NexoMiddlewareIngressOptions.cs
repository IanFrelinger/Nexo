using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

/// <summary>
/// Feature flags for optional ingress surfaces (WebSocket labs, SMS simulation webhook, tenant/app guardrails).
/// </summary>
public sealed class NexoMiddlewareIngressOptions
{
    /// <summary>Configuration section path (<c>Nexo:MiddlewareIngress</c>).</summary>
    public const string SectionPath = "Nexo:MiddlewareIngress";

    /// <summary>When true, exposes the WebSocket lab echo endpoint.</summary>
    public bool EnableWebSocketIngress { get; set; } = true;

    /// <summary>When true, exposes POST /api/ingress/sms/simulate for labs (not a substitute for signed SNS/Lambda).</summary>
    public bool EnableSmsSimulationIngress { get; set; }

    /// <summary>
    /// When non-empty, SMS simulation requests must include matching <c>X-Nexo-App-Id</c> header or body app id field.
    /// </summary>
    public string[] SmsSimulationAllowedAppIds { get; set; } = [];

    /// <summary>
    /// Capability keys disabled globally for guarded ingress routes.
    /// Known keys: <c>websocket</c>, <c>sms-simulation</c>, <c>aws-sns-sms</c>.
    /// </summary>
    public string[] DisabledCapabilities { get; set; } = [];

    /// <summary>
    /// When true, exposes POST /api/ingress/sms/sns for Amazon SNS HTTP(S) callbacks (signature verification required unless Testing skip flag is set).
    /// </summary>
    public bool EnableAwsSnsSmsWebhook { get; set; }

    /// <summary>
    /// When non-empty, <c>TopicArn</c> on SNS notifications must start with one of these prefixes (spoof protection).
    /// </summary>
    public string[] AwsSnsAllowedTopicArnPrefixes { get; set; } = [];

    /// <summary>
    /// When non-empty, <c>X-Nexo-App-Id</c> must match one of these values for the SNS webhook (parity with SMS simulation).
    /// </summary>
    public string[] AwsSnsAllowedAppIds { get; set; } = [];

    /// <summary>
    /// When true, after a valid signature on Subscription/Unsubscribe confirmation, performs HTTPS GET on <c>SubscribeURL</c> (use with care).
    /// </summary>
    public bool AwsSnsAutoConfirmSubscription { get; set; }

    /// <summary>
    /// When true and the host environment name is <c>Testing</c>, skips SNS RSA verification (integration tests only).
    /// </summary>
    public bool AwsSnsSkipSignatureVerification { get; set; }

    /// <summary>
    /// Certificate revocation policy for SNS signing certificate chain builds: <c>NoCheck</c> (default), <c>Online</c>, or <c>Offline</c>.
    /// </summary>
    public string AwsSnsSigningCertificateRevocationMode { get; set; } = "NoCheck";

    /// <summary>
    /// When greater than zero, applies a per-IP fixed-window rate limit to <c>POST /api/ingress/sms/simulate</c> and <c>POST /api/ingress/sms/sns</c>.
    /// </summary>
    public int IngressSmsPostRateLimitPermitLimit { get; set; }

    /// <summary>Window length in seconds for <see cref="IngressSmsPostRateLimitPermitLimit"/> (default 60).</summary>
    public int IngressSmsPostRateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Backing store for SMS approvals: <see cref="SmsIngressApprovalStoreKind.Memory"/> or <see cref="SmsIngressApprovalStoreKind.DynamoDb"/>.
    /// </summary>
    public string SmsIngressApprovalStore { get; set; } = SmsIngressApprovalStoreKind.Memory;

    /// <summary>
    /// Optional per-tenant allowlists for guarded capabilities (fail closed when the tenant header matches a key).
    /// Keys are matched against <c>X-Nexo-Tenant</c>.
    /// </summary>
    public Dictionary<string, string[]> TenantCapabilityAllowlists { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
