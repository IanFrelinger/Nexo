namespace Ashlar.Orchestration.Routing;

/// <summary>
/// Raised when no healthy endpoint is available for routing.
/// </summary>
public sealed class EndpointUnavailableException : Exception
{
    public EndpointUnavailableException(
        IReadOnlyList<string> unhealthyEndpoints,
        string message)
        : base(message)
    {
        UnhealthyEndpoints = unhealthyEndpoints;
    }

    public IReadOnlyList<string> UnhealthyEndpoints { get; }
}
