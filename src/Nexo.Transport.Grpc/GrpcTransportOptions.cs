namespace Nexo.Transport.Grpc;

/// <summary>
/// Configuration for gRPC transport channel creation and TLS behavior.
/// </summary>
public sealed class GrpcTransportOptions
{
    /// <summary>
    /// Allows non-TLS gRPC endpoints. This must only be enabled in development.
    /// </summary>
    public bool AllowInsecure { get; init; }

    /// <summary>
    /// Maximum retry attempts used by gRPC transport callers.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Optional PEM/CRT path to client certificate for mTLS.
    /// </summary>
    public string? ClientCertPath { get; init; }

    /// <summary>
    /// Optional PEM key path paired with <see cref="ClientCertPath"/> for mTLS.
    /// </summary>
    public string? ClientCertKeyPath { get; init; }

    /// <summary>
    /// Optional custom CA certificate path for private or air-gapped PKI.
    /// </summary>
    public string? CaCertPath { get; init; }

    /// <summary>
    /// Validates security-sensitive options against the current environment.
    /// </summary>
    /// <param name="isDevelopment">True when running in development mode.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when insecure transport is requested outside development.
    /// </exception>
    public void Validate(bool isDevelopment)
    {
        if (AllowInsecure && !isDevelopment)
        {
            throw new InvalidOperationException(
                "GrpcTransportOptions.AllowInsecure cannot be true outside development.");
        }
    }
}
