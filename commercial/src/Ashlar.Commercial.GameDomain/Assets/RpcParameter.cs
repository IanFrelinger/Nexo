namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Rpc parameter record.</summary>
public sealed record RpcParameter
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Type value.</summary>
    public string Type { get; init; } = "float";
}
