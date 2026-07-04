using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

/// <summary>Phase A inventory — canonical HTTP/WebSocket/SMS lab seams (no secrets).</summary>
public static class IngressCatalog
{
    /// <summary>Static catalog of supported ingress surfaces and lab seams.</summary>
    public static IngressCatalogResponse Current { get; } = new(
    [
        new IngressCatalogEntry("HttpApi", "/api/*", "Primary REST ingress via MapNexoEndpoints (MediatR-backed handlers)."),
        new IngressCatalogEntry("Health", "/health", "Liveness probe."),
        new IngressCatalogEntry("OpenApi", "/swagger/v1/swagger.json", "Swagger/OpenAPI document when enabled."),
        new IngressCatalogEntry("MiddlewareIngress", "/api/middleware/*", "Correlation echo, ingress catalog, ingress context snapshot."),
        new IngressCatalogEntry("WebSocketLab", "/ws/v1/echo", "Optional lab echo (feature-flagged)."),
        new IngressCatalogEntry("SmsSimulation", "/api/ingress/sms/simulate", "Optional inbound SMS keyword lab (feature-flagged)."),
        new IngressCatalogEntry("AwsSnsSms", "/api/ingress/sms/sns", "Amazon SNS HTTP(S) webhook (optional; verify signatures in production)."),
        new IngressCatalogEntry("GrpcTransportHost", "(separate host)", "See Nexo.Transport.Grpc.Server.Host — not co-hosted in Nexo.API."),
    ]);
}
