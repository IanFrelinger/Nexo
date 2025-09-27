using System.ComponentModel.DataAnnotations;

namespace Nexo.Observability.Configuration;

/// <summary>
/// Configuration options for OpenTelemetry observability.
/// </summary>
public partial class ObservabilityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Observability";

    /// <summary>
    /// Service name for OpenTelemetry resource.
    /// </summary>
    public string ServiceName { get; set; } = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "Nexo";

    /// <summary>
    /// Service version for OpenTelemetry resource.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Resource attributes from OTEL_RESOURCE_ATTRIBUTES.
    /// </summary>
    public Dictionary<string, string> ResourceAttributes { get; set; } = ParseResourceAttributes();

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

    /// <summary>
    /// Parses OTEL_RESOURCE_ATTRIBUTES environment variable.
    /// </summary>
    /// <returns>Dictionary of resource attributes.</returns>
    private static Dictionary<string, string> ParseResourceAttributes()
    {
        var attributes = new Dictionary<string, string>();
        var resourceAttributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        
        if (!string.IsNullOrEmpty(resourceAttributes))
        {
            var pairs = resourceAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    attributes[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
        }
        
        return attributes;
    }
}

/// <summary>
/// Sampling configuration options.
/// </summary>
public partial class SamplingOptions
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
public partial class ConsoleExporterOptions
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
public partial class OtlpExporterOptions
{
    /// <summary>
    /// Whether to enable OTLP exporter.
    /// </summary>
    public bool Enabled { get; set; } = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

    /// <summary>
    /// OTLP endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; } = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

    /// <summary>
    /// OTLP headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Protocol (grpc or http/protobuf).
    /// </summary>
    public OtlpProtocol Protocol { get; set; } = ParseProtocol();
    
    /// <summary>
    /// Parses OTEL_EXPORTER_OTLP_PROTOCOL environment variable.
    /// </summary>
    /// <returns>OTLP protocol type.</returns>
    private static OtlpProtocol ParseProtocol()
    {
        var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")?.ToLowerInvariant();
        return protocol switch
        {
            "http" or "http/protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => OtlpProtocol.Grpc
        };
    }
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
