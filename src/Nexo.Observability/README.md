# Nexo Observability

This module provides comprehensive OpenTelemetry-based observability for the Nexo system, including distributed tracing, metrics, and logging integration.

## Features

- **Distributed Tracing**: Activity-based tracing for generation, validation, policy, pipeline, and repair operations
- **Metrics**: Custom counters and histograms for pipeline performance and quality metrics
- **Multiple Exporters**: Console and OTLP exporters with configurable settings
- **Sampling**: Configurable sampling strategies (AlwaysOn, AlwaysOff, TraceIdRatioBased)
- **Resource Configuration**: Automatic service identification and versioning

## Quick Start

### 1. Add the Package

The observability module is automatically included when you reference `Nexo.Core`. No additional package references are needed.

### 2. Configure Services

```csharp
using Nexo.Observability;

var services = new ServiceCollection();
services.AddNexoObservability(configuration);
```

### 3. Configuration

Add observability settings to your `appsettings.json`:

```json
{
  "Observability": {
    "ServiceName": "Nexo",
    "ServiceVersion": "1.0.0",
    "Sampling": {
      "Type": "TraceIdRatioBased",
      "Ratio": 0.1
    },
    "Console": {
      "Enabled": true,
      "IncludeScopes": true,
      "IncludeActivityTags": true
    },
    "Otlp": {
      "Enabled": false,
      "Endpoint": "http://localhost:4317",
      "Protocol": "Grpc",
      "Headers": {
        "Authorization": "Bearer your-token-here"
      }
    }
  }
}
```

## Activity Sources

The module provides the following activity sources:

- `Nexo.Generation` - Code generation operations
- `Nexo.Validation` - Validation and compilation gates
- `Nexo.Policy` - Policy evaluation and scoring
- `Nexo.Pipeline` - Overall pipeline orchestration
- `Nexo.Repair` - Repair and recovery operations
- `Nexo.Observability` - Observability infrastructure

## Metrics

The following metrics are automatically collected:

### Counters
- `nexo.generation.duration` - Generation operation duration (ms)
- `nexo.validation.failures` - Number of validation failures
- `nexo.policy.score` - Policy evaluation scores
- `nexo.pipeline.success` - Successful pipeline executions
- `nexo.pipeline.failure` - Failed pipeline executions

### Histograms
- `nexo.generation.duration.histogram` - Distribution of generation durations
- `nexo.validation.duration.histogram` - Distribution of validation durations
- `nexo.policy.evaluation.duration.histogram` - Distribution of policy evaluation durations
- `nexo.pipeline.duration.histogram` - Distribution of pipeline execution durations

## Usage in Components

### Using Activity Sources

```csharp
public class MyService
{
    private readonly ActivitySource _generationActivitySource;
    
    public MyService(ActivitySource generationActivitySource)
    {
        _generationActivitySource = generationActivitySource;
    }
    
    public async Task<string> GenerateCodeAsync()
    {
        using var activity = _generationActivitySource.StartActivity("code.generation");
        activity?.SetTag("language", "csharp");
        
        try
        {
            // Your generation logic here
            var result = await DoGenerationAsync();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### Using Metrics

```csharp
public class MyService
{
    private readonly PipelineMetrics _metrics;
    
    public MyService(PipelineMetrics metrics)
    {
        _metrics = metrics;
    }
    
    public async Task ProcessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Your processing logic here
            await DoProcessingAsync();
            
            stopwatch.Stop();
            _metrics.RecordPipelineSuccess("my_pipeline", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordPipelineFailure("my_pipeline", stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
```

## Configuration Options

### Sampling Types

- `AlwaysOn` - Sample all traces (default)
- `AlwaysOff` - Sample no traces
- `TraceIdRatioBased` - Sample based on trace ID hash ratio

### OTLP Protocols

- `Grpc` - gRPC protocol (default)
- `HttpProtobuf` - HTTP/Protobuf protocol

### Environment Variables

You can override configuration using environment variables:

```bash
export Observability__Otlp__Enabled=true
export Observability__Otlp__Endpoint=http://jaeger:4317
export Observability__Sampling__Ratio=0.1
```

## Integration with External Systems

### Jaeger

```json
{
  "Observability": {
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://jaeger:4317",
      "Protocol": "Grpc"
    }
  }
}
```

### Zipkin

```json
{
  "Observability": {
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://zipkin:9411/api/v2/spans",
      "Protocol": "HttpProtobuf"
    }
  }
}
```

### Prometheus

The metrics are automatically exposed in OpenTelemetry format and can be scraped by Prometheus using the OpenTelemetry Collector.

## Testing

The module includes comprehensive tests that verify:

- Activity emission for generation steps
- Metrics recording functionality
- OTLP configuration without endpoint (no exceptions)
- Service registration and dependency injection

Run the tests:

```bash
dotnet test src/Nexo.Observability.Tests/
```

## Examples

See the `examples/` directory for complete usage examples:

- `ObservabilityUsageExample.cs` - Basic usage demonstration
- `observability-config-example.json` - Configuration example

## Troubleshooting

### No Activities Appearing

1. Ensure the activity source is registered in your DI container
2. Check that sampling is not set to `AlwaysOff`
3. Verify that the activity source name matches the registered source

### No Metrics

1. Ensure the `PipelineMetrics` service is registered
2. Check that the meter is properly configured
3. Verify that metrics are being recorded in your code

### OTLP Export Issues

1. Check that the OTLP endpoint is accessible
2. Verify authentication headers if required
3. Ensure the protocol matches your collector configuration
