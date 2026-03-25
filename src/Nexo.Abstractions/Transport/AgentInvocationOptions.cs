namespace Nexo.Abstractions.Transport;

/// <summary>
/// Transport execution options for a single agent invocation.
/// </summary>
/// <param name="Timeout">Maximum allowed execution time for this invocation.</param>
/// <param name="MaxRetries">Maximum retries transport may attempt for transient failures.</param>
/// <param name="TargetEndpoint">
/// Optional target endpoint for remote execution. Null means execute in-process.
/// </param>
public sealed record AgentInvocationOptions(
    TimeSpan Timeout,
    int MaxRetries,
    string? TargetEndpoint = null)
{
    /// <summary>
    /// Default invocation options used when none are explicitly provided.
    /// </summary>
    public static AgentInvocationOptions Default { get; } = new(
        Timeout: TimeSpan.FromSeconds(30),
        MaxRetries: 3,
        TargetEndpoint: null);
}
