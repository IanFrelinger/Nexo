using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nexo.Observability;
using Nexo.Observability.Configuration;
using Xunit;

namespace Nexo.Observability.Tests;

/// <summary>
/// Tests for OTLP configuration scenarios.
/// </summary>
public class OtlpConfigurationTests
{
    [Fact]
    public void AddNexoObservability_WithOtlpDisabled_ShouldNotThrow()
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

        // Act & Assert
        // This should not throw even without OTLP endpoint
        services.AddNexoObservability(configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.False(options.Value.Otlp.Enabled);
    }

    [Fact]
    public void AddNexoObservability_WithOtlpEnabledButNoEndpoint_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:ServiceName"] = "TestService",
                ["Observability:ServiceVersion"] = "1.0.0-test",
                ["Observability:Console:Enabled"] = "true",
                ["Observability:Otlp:Enabled"] = "true"
                // Note: No OTLP endpoint specified
            })
            .Build();

        // Act & Assert
        // This should not throw even without OTLP endpoint
        services.AddNexoObservability(configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.True(options.Value.Otlp.Enabled);
        Assert.Null(options.Value.Otlp.Endpoint);
    }

    [Fact]
    public void AddNexoObservability_WithOtlpHttpProtobuf_ShouldConfigureCorrectly()
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
                ["Observability:Otlp:Endpoint"] = "http://localhost:4318",
                ["Observability:Otlp:Protocol"] = "HttpProtobuf"
            })
            .Build();

        // Act
        services.AddNexoObservability(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.True(options.Value.Otlp.Enabled);
        Assert.Equal("http://localhost:4318", options.Value.Otlp.Endpoint);
        Assert.Equal(OtlpProtocol.HttpProtobuf, options.Value.Otlp.Protocol);
    }

    [Fact]
    public void AddNexoObservability_WithOtlpHeaders_ShouldConfigureCorrectly()
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
                ["Observability:Otlp:Protocol"] = "Grpc",
                ["Observability:Otlp:Headers:Authorization"] = "Bearer test-token",
                ["Observability:Otlp:Headers:X-Custom-Header"] = "custom-value"
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
        Assert.Equal("Bearer test-token", options.Value.Otlp.Headers["Authorization"]);
        Assert.Equal("custom-value", options.Value.Otlp.Headers["X-Custom-Header"]);
    }

    [Fact]
    public void AddNexoObservability_WithDefaultConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddNexoObservability(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ObservabilityOptions>>();
        
        Assert.NotNull(options);
        Assert.Equal("Nexo", options.Value.ServiceName);
        Assert.Equal("1.0.0", options.Value.ServiceVersion);
        Assert.True(options.Value.Console.Enabled);
        Assert.False(options.Value.Otlp.Enabled);
        Assert.Equal(SamplingType.AlwaysOn, options.Value.Sampling.Type);
        Assert.Equal(1.0, options.Value.Sampling.Ratio);
    }
}
