namespace Ashlar.API.Middleware.Ingress;

/// <summary>Ingress capability.</summary>
public static class IngressCapability
{
    /// <summary>WebSocket lab ingress capability key.</summary>
    public const string WebSocket = "websocket";

    /// <summary>SMS simulation ingress capability key.</summary>
    public const string SmsSimulation = "sms-simulation";

    /// <summary>AWS SNS SMS webhook ingress capability key.</summary>
    public const string AwsSnsSms = "aws-sns-sms";

    /// <summary>MCP server ingress capability key (AI clients calling Ashlar tools).</summary>
    public const string Mcp = "mcp";

    /// <summary>A2A server ingress capability key (external agents delegating to Ashlar agents).</summary>
    public const string A2A = "a2a";
}
