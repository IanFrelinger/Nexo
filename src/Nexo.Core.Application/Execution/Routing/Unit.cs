namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Unit value for successful operations that return no payload.
/// </summary>
public readonly struct Unit
{
    /// <summary>Singleton unit value.</summary>
    public static Unit Value => default;
}
