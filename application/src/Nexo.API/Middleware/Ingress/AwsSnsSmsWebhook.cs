using MediatR;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nexo.Contracts;
using Nexo.Ingress.AwsSns;

namespace Nexo.API.Middleware.Ingress;

/// <summary>HTTP POST handler for Amazon SNS subscription and notification payloads.</summary>
public static class AwsSnsSmsWebhook
{
    /// <summary>Maps the Amazon SNS SMS webhook endpoint when enabled.</summary>
    public static void MapAwsSnsSmsWebhook(this WebApplication app)
    {
        app.MapPost("/api/ingress/sms/sns", HandleAwsSnsAsync)
            .WithTags("MiddlewareIngress")
            .WithName("AwsSnsSmsWebhook")
            .WithSummary("Amazon SNS HTTP(S) webhook for SMS-derived approvals (signature verification + YES token)")
            .RequireRateLimiting("nexo-sms-ingress-posts");
    }

    private static async Task<IResult> HandleAwsSnsAsync(
        HttpContext httpContext,
        [FromServices] IOptionsMonitor<NexoMiddlewareIngressOptions> optsMonitor,
        [FromServices] ISnsSignatureVerifier signatureVerifier,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IHostEnvironment hostEnvironment,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var options = optsMonitor.CurrentValue;
        if (!options.EnableAwsSnsSmsWebhook)
            return Results.NotFound();

        if (IngressEndpointPolicies.IsCapabilityDisabled(options, IngressCapability.AwsSnsSms))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var tenantId = httpContext.Request.Headers["X-Nexo-Tenant"].FirstOrDefault();
        if (!IngressEndpointPolicies.TenantAllowsCapability(options, tenantId, IngressCapability.AwsSnsSms))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var appId = httpContext.Request.Headers["X-Nexo-App-Id"].FirstOrDefault();
        if (options.AwsSnsAllowedAppIds.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(appId)
                || !options.AwsSnsAllowedAppIds.Contains(appId.Trim(), StringComparer.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { title = "Invalid JSON", detail = "Body must be a JSON document." });
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("Type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
                return Results.BadRequest(new { title = "Invalid SNS message", detail = "Missing Type." });

            var type = typeEl.GetString() ?? string.Empty;
            var http = httpClientFactory.CreateClient("nexo-sns-signing");
            var skipSig = options.AwsSnsSkipSignatureVerification && hostEnvironment.IsEnvironment("Testing");
            if (!await signatureVerifier.IsAuthenticAsync(
                root,
                http,
                skipSig,
                SnsRevocationModeParser.Parse(options.AwsSnsSigningCertificateRevocationMode),
                cancellationToken).ConfigureAwait(false))
                return Results.Unauthorized();

            if (root.TryGetProperty("TopicArn", out var topicProp) && topicProp.ValueKind == JsonValueKind.String)
            {
                var arn = topicProp.GetString();
                if (!TopicArnAllowed(arn, options.AwsSnsAllowedTopicArnPrefixes))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
            else if (options.AwsSnsAllowedTopicArnPrefixes.Length > 0)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return type switch
            {
                "SubscriptionConfirmation" or "UnsubscribeConfirmation" => await HandleConfirmationAsync(
                    root,
                    type,
                    options,
                    http,
                    cancellationToken).ConfigureAwait(false),
                "Notification" => await HandleNotificationAsync(root, mediator, cancellationToken).ConfigureAwait(false),
                _ => Results.Ok(new { acknowledged = true, type })
            };
        }
    }

    private static async Task<IResult> HandleConfirmationAsync(
        JsonElement root,
        string type,
        NexoMiddlewareIngressOptions options,
        HttpClient http,
        CancellationToken cancellationToken)
    {
        if (options.AwsSnsAutoConfirmSubscription
            && root.TryGetProperty("SubscribeURL", out var sub) && sub.ValueKind == JsonValueKind.String)
        {
            var url = sub.GetString();
            if (!string.IsNullOrWhiteSpace(url)
                && Uri.TryCreate(url, UriKind.Absolute, out var confirmUri)
                && string.Equals(confirmUri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
                && IsTrustedSnsHost(confirmUri.Host))
            {
                using var response = await http.GetAsync(confirmUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return Results.Ok(new { acknowledged = true, snsType = type });
    }

    private static async Task<IResult> HandleNotificationAsync(
        JsonElement root,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("Message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String)
            return Results.BadRequest(new { title = "Invalid notification", detail = "Missing Message." });

        var rawMessage = msgEl.GetString() ?? string.Empty;
        if (!SnsSmsMessageExtractor.TryExtract(rawMessage, out var fromAddr, out var bodyText))
            return Results.BadRequest(new { title = "Invalid notification", detail = "Empty Message." });

        var sid = root.TryGetProperty("MessageId", out var mid) && mid.ValueKind == JsonValueKind.String
            ? mid.GetString()
            : null;

        if (!SmsYesTokenParser.TryParse(bodyText, out var token))
        {
            return Results.Ok(new SmsInboundSimulationResponse(false, null, "Body must match \"YES <token>\".", false));
        }

        var from = string.IsNullOrWhiteSpace(fromAddr) ? "sns" : fromAddr.Trim();
        var response = await mediator
            .Send(new RecordSmsYesApprovalCommand(from, token, sid), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(response);
    }

    private static bool TopicArnAllowed(string? topicArn, string[] allowedPrefixes)
    {
        if (allowedPrefixes.Length == 0)
            return true;
        if (string.IsNullOrWhiteSpace(topicArn))
            return false;
        return allowedPrefixes.Any(p => topicArn.StartsWith(p.Trim(), StringComparison.Ordinal));
    }

    private static bool IsTrustedSnsHost(string host) =>
        host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase);
}
