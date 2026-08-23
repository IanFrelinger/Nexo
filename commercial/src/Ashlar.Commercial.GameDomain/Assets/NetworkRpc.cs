namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Network rpc record.</summary>
public sealed record NetworkRpc
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Direction value.</summary>
    public string Direction { get; init; } = "server"; // server (client→server), client (server→client)
    /// <summary>Delivery mode value.</summary>
    public string DeliveryMode { get; init; } = "reliable";
    /// <summary>Parameters value.</summary>
    public IReadOnlyList<RpcParameter> Parameters { get; init; } = Array.Empty<RpcParameter>();
}
