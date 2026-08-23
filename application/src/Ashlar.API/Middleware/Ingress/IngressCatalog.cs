using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>Phase A inventory — canonical HTTP/WebSocket/SMS lab seams (no secrets).</summary>
public static class IngressCatalog
{
    /// <summary>Static catalog of supported ingress surfaces and lab seams.</summary>
    public static IngressCatalogResponse Current { get; } = new(
    [
        new IngressCatalogEntry("HttpApi", "/api/*", "Primary REST ingress via MapAshlarEndpoints (MediatR-backed handlers)."),
        new IngressCatalogEntry("Health", "/health", "Liveness probe."),
        new IngressCatalogEntry("Ready", "/ready", "Readiness probe (503 while starting or stopping)."),
        new IngressCatalogEntry("OpenApi", "/swagger/v1/swagger.json", "Swagger/OpenAPI document when enabled."),
        new IngressCatalogEntry("MiddlewareIngress", "/api/middleware/*", "Correlation echo, ingress catalog, ingress context snapshot."),
        new IngressCatalogEntry("WebSocketLab", "/ws/v1/echo", "Optional lab echo (feature-flagged)."),
        new IngressCatalogEntry("SmsSimulation", "/api/ingress/sms/simulate", "Optional inbound SMS keyword lab (feature-flagged)."),
        new IngressCatalogEntry("AwsSnsSms", "/api/ingress/sms/sns", "Amazon SNS HTTP(S) webhook (optional; verify signatures in production)."),
        new IngressCatalogEntry("GrpcTransportHost", "(separate host)", "See Ashlar.Transport.Grpc.Server.Host — not co-hosted in Ashlar.API."),
        new IngressCatalogEntry("McpServer", "/api/mcp", "MCP streamable-HTTP server (feature-flagged, allowlisted tools; stdio variant via Ashlar.Mcp.Server.Host)."),
        new IngressCatalogEntry("A2AAgents", "/api/a2a/{agentId} + /.well-known/agent-card.json", "A2A v1.0 JSON-RPC agent endpoints and discovery cards (feature-flagged, allowlisted agents)."),
    ]);
}
