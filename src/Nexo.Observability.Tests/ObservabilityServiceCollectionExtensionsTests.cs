using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Observability;
using Nexo.Observability.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace Nexo.Observability.Tests;

/// <summary>
/// Tests for the ObservabilityServiceCollectionExtensions.
/// </summary>
public partial class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNexoObservability_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ServiceName"] = "TestService",
                ["Observability:ServiceVersion"] = "1.0.0-test",
                ["Observability:Console:Enabled"] = "true",
                ["Observability:Otlp:Enabled"] = "false"
            })
            .Build();

        // Act
        services.AddNexoObservability(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that observability options are registered
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        Assert.NotNull(options);
        Assert.Equal("TestService", options.Value.ServiceName);
        Assert.Equal("1.0.0-test", options.Value.ServiceVersion);
        Assert.True(options.Value.Console.Enabled);
        Assert.False(options.Value.Otlp.Enabled);

        // Check that activity sources are registered
        var pipelineActivitySource = serviceProvider.GetService<ActivitySource>();
        Assert.NotNull(pipelineActivitySource);
        Assert.Equal("Nexo", pipelineActivitySource.Name);

        // Check that metrics are registered
        var metrics = serviceProvider.GetService<PipelineMetrics>();
        Assert.NotNull(metrics);

        // Check that individual activity sources are registered
        var generationSource = serviceProvider.GetServices<ActivitySource>().FirstOrDefault(s => s.Name == "Nexo.Generation");
        Assert.NotNull(generationSource);
    }

    [Fact]
    public void AddNexoObservability_WithOtlpEnabled_ShouldConfigureOtlpExporter()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ServiceName"] = "TestService",
                ["Observability:ServiceVersion"] = "1.0.0-test",
                ["Observability:Console:Enabled"] = "false",
                ["Observability:Otlp:Enabled"] = "true",
                ["Observability:Otlp:Endpoint"] = "http://localhost:4317",
                ["Observability:Otlp:Protocol"] = "Grpc"
            })
            .Build();

        // Act
        services.AddNexoObservability(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.True(options.Value.Otlp.Enabled);
        Assert.Equal("http://localhost:4317", options.Value.Otlp.Endpoint);
        Assert.Equal(OtlpProtocol.Grpc, options.Value.Otlp.Protocol);
    }

    [Fact]
    public void AddNexoObservability_WithSamplingConfiguration_ShouldConfigureSampling()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ServiceName"] = "TestService",
                ["Observability:ServiceVersion"] = "1.0.0-test",
                ["Observability:Sampling:Type"] = "TraceIdRatioBased",
                ["Observability:Sampling:Ratio"] = "0.5"
            })
            .Build();

        // Act
        services.AddNexoObservability(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.Equal(SamplingType.TraceIdRatioBased, options.Value.Sampling.Type);
        Assert.Equal(0.5, options.Value.Sampling.Ratio);
    }
}
