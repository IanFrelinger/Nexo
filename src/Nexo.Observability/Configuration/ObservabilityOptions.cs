using System.ComponentModel.DataAnnotations;

namespace Nexo.Observability.Configuration;

/// <summary>
/// Configuration options for OpenTelemetry observability.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Observability";

    /// <summary>
    /// Service name for OpenTelemetry resource.
    /// </summary>
    public string ServiceName { get; set; } = "Nexo";

    /// <summary>
    /// Service version for OpenTelemetry resource.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Sampling configuration.
    /// </summary>
    public SamplingOptions Sampling { get; set; } = new();

    /// <summary>
    /// Console exporter configuration.
    /// </summary>
    public ConsoleExporterOptions Console { get; set; } = new();

    /// <summary>
    /// OTLP exporter configuration.
    /// </summary>
    public OtlpExporterOptions Otlp { get; set; } = new();
}

/// <summary>
/// Sampling configuration options.
/// </summary>
public class SamplingOptions
{
    /// <summary>
    /// Sampling ratio (0.0 to 1.0).
    /// </summary>
    [Range(0.0, 1.0)]
    public double Ratio { get; set; } = 1.0;

    /// <summary>
    /// Sampling type.
    /// </summary>
    public SamplingType Type { get; set; } = SamplingType.AlwaysOn;
}

/// <summary>
/// Sampling types.
/// </summary>
public enum SamplingType
{
    /// <summary>
    /// Always sample.
    /// </summary>
    AlwaysOn,

    /// <summary>
    /// Never sample.
    /// </summary>
    AlwaysOff,

    /// <summary>
    /// Sample based on ratio.
    /// </summary>
    TraceIdRatioBased
}

/// <summary>
/// Console exporter configuration.
/// </summary>
public class ConsoleExporterOptions
{
    /// <summary>
    /// Whether to enable console exporter.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to include scopes in console output.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Whether to include activity tags in console output.
    /// </summary>
    public bool IncludeActivityTags { get; set; } = true;
}

/// <summary>
/// OTLP exporter configuration.
/// </summary>
public class OtlpExporterOptions
{
    /// <summary>
    /// Whether to enable OTLP exporter.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// OTLP endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// OTLP headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Protocol (grpc or http/protobuf).
    /// </summary>
    public OtlpProtocol Protocol { get; set; } = OtlpProtocol.Grpc;
}

/// <summary>
/// OTLP protocol types.
/// </summary>
public enum OtlpProtocol
{
    /// <summary>
    /// gRPC protocol.
    /// </summary>
    Grpc,

    /// <summary>
    /// HTTP/Protobuf protocol.
    /// </summary>
    HttpProtobuf
}
