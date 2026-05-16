using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nexo.Contracts;
using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.API.Middleware.Ingress;

public static class IngressEndpoints
{
    public static WebApplication MapIngressEndpoints(this WebApplication app)
    {
        var ingressGroup = app.MapGroup("/api/middleware").WithTags("MiddlewareIngress");

        ingressGroup.MapGet("/correlation-echo", (HttpContext ctx) =>
            {
                var id = ctx.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var v) && v is string s
                    ? s
                    : string.Empty;
                return Results.Ok(new { correlationId = id });
            })
            .WithName("IngressCorrelationEcho")
            .WithSummary("Echoes the active correlation id for this HTTP request");

        ingressGroup.MapGet("/ingress-catalog", () => Results.Ok(IngressCatalog.Current))
            .WithName("IngressCatalog")
            .WithSummary("Operator inventory of ingress seams (Phase A)");

        ingressGroup.MapGet("/ingress-context", (INexoIngressAccessor ingress) =>
                Results.Ok(new IngressContextResponse(
                    ingress.CorrelationId,
                    ingress.Transport,
                    ingress.TenantId,
                    ingress.AppId,
                    ingress.IdempotencyKey,
                    ingress.PayloadVersion)))
            .WithName("IngressContext")
            .WithSummary("Snapshot of ingress headers mapped for this request (MediatR logging scope source)");

        app.MapPost("/api/ingress/sms/simulate", HandleSmsSimulateAsync)
            .WithTags("MiddlewareIngress")
            .WithName("SmsInboundSimulate")
            .WithSummary("Lab-only simulated inbound SMS approval line (keyword YES &lt;token&gt;)")
            .RequireRateLimiting("nexo-sms-ingress-posts");

        app.Map("/ws/v1/echo", HandleWebSocketEchoAsync);
        app.MapAwsSnsSmsWebhook();

        return app;
    }

    private static async Task<IResult> HandleSmsSimulateAsync(
        HttpContext httpContext,
        [FromServices] IOptionsMonitor<NexoMiddlewareIngressOptions> optsMonitor,
        [FromServices] IMediator mediator,
        [FromBody] SmsInboundSimulationRequest? body,
        CancellationToken cancellationToken)
    {
        var options = optsMonitor.CurrentValue;
        if (!options.EnableSmsSimulationIngress)
            return Results.NotFound();

        if (IngressEndpointPolicies.IsCapabilityDisabled(options, IngressCapability.SmsSimulation))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var tenantId = httpContext.Request.Headers["X-Nexo-Tenant"].FirstOrDefault();
        if (!IngressEndpointPolicies.TenantAllowsCapability(options, tenantId, IngressCapability.SmsSimulation))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        if (body is null || string.IsNullOrWhiteSpace(body.From) || string.IsNullOrWhiteSpace(body.Body))
            return Results.BadRequest(new { title = "Invalid body", detail = "From and Body are required." });

        var appId = body.AppId ?? httpContext.Request.Headers["X-Nexo-App-Id"].FirstOrDefault();
        if (options.SmsSimulationAllowedAppIds.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(appId)
                || !options.SmsSimulationAllowedAppIds.Contains(appId.Trim(), StringComparer.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        if (!SmsYesTokenParser.TryParse(body.Body, out var token))
        {
            return Results.Ok(new SmsInboundSimulationResponse(false, null, "Body must match \"YES <token>\".", false));
        }

        var response = await mediator
            .Send(new RecordSmsYesApprovalCommand(body.From.Trim(), token, body.MessageSid), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(response);
    }

    private static async Task HandleWebSocketEchoAsync(HttpContext context)
    {
        var opts = context.RequestServices.GetRequiredService<IOptionsMonitor<NexoMiddlewareIngressOptions>>().CurrentValue;
        if (!opts.EnableWebSocketIngress || IngressEndpointPolicies.IsCapabilityDisabled(opts, IngressCapability.WebSocket))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var tenantHeader = context.Request.Headers["X-Nexo-Tenant"].FirstOrDefault();
        if (!IngressEndpointPolicies.TenantAllowsCapability(opts, tenantHeader, IngressCapability.WebSocket))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected WebSocket upgrade.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

        var correlation =
            context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var c) && c is string cs
                ? cs
                : Guid.NewGuid().ToString("N");

        var hello = JsonSerializer.Serialize(new { type = "hello", correlationId = correlation, transport = NexoIngressTransports.WebSocket });
        await SendTextAsync(socket, hello, context.RequestAborted).ConfigureAwait(false);

        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer.AsMemory(), context.RequestAborted).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", context.RequestAborted).ConfigureAwait(false);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
                var echo = JsonSerializer.Serialize(new { type = "echo", correlationId = correlation, text });
                await SendTextAsync(socket, echo, context.RequestAborted).ConfigureAwait(false);
            }
        }
    }

    private static async Task SendTextAsync(WebSocket socket, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }
}
