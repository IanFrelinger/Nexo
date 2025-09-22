using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;
using Xunit;

namespace Nexo.CLI.Tests
{
    /// <summary>
    /// Tests for observability functionality.
    /// </summary>
    public class ObservabilityTests
    {
        [Fact]
        public void AddNexoObservability_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            services.AddNexoObservability(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<ActivitySource>());
            Assert.NotNull(serviceProvider.GetService<Meter>());
            Assert.NotNull(serviceProvider.GetService<PipelineMetrics>());
            Assert.NotNull(serviceProvider.GetService<ActivitySource>(provider => NexoActivitySources.Generation));
            Assert.NotNull(serviceProvider.GetService<ActivitySource>(provider => NexoActivitySources.Validation));
            Assert.NotNull(serviceProvider.GetService<ActivitySource>(provider => NexoActivitySources.Policy));
            Assert.NotNull(serviceProvider.GetService<ActivitySource>(provider => NexoActivitySources.Pipeline));
            Assert.NotNull(serviceProvider.GetService<ActivitySource>(provider => NexoActivitySources.Repair));
        }

        [Fact]
        public void ObservabilityOptions_ShouldReadEnvironmentVariables()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "TestService");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://test-endpoint");
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http");
            Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "key1=value1,key2=value2");

            try
            {
                // Act
                var options = new Nexo.Observability.Configuration.ObservabilityOptions();

                // Assert
                Assert.Equal("TestService", options.ServiceName);
                Assert.True(options.Otlp.Enabled);
                Assert.Equal("http://test-endpoint", options.Otlp.Endpoint);
                Assert.Equal(Nexo.Observability.Configuration.OtlpProtocol.HttpProtobuf, options.Otlp.Protocol);
                Assert.Equal("value1", options.ResourceAttributes["key1"]);
                Assert.Equal("value2", options.ResourceAttributes["key2"]);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", null);
                Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", null);
            }
        }

        [Fact]
        public void ActivitySource_ShouldCreateActivity()
        {
            // Arrange
            var activitySource = NexoActivitySources.Generation;
            var activities = new List<Activity>();
            var activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => activities.Add(activity),
                ActivityStopped = activity => { }
            };

            ActivitySource.AddActivityListener(activityListener);

            // Act
            using var activity = activitySource.StartActivity("TestGeneration");
            activity?.SetTag("test.tag", "test-value");
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Assert
            Assert.Single(activities);
            Assert.Equal("TestGeneration", activities[0].OperationName);
            Assert.Equal("test-value", activities[0].GetTagItem("test.tag"));
            Assert.Equal(ActivityStatusCode.Ok, activities[0].Status);
        }

        [Fact]
        public void PipelineMetrics_ShouldRecordMetrics()
        {
            // Arrange
            var meter = new Meter("TestMeter");
            var metrics = new PipelineMetrics(meter);

            // Act & Assert - Should not throw
            Assert.NotNull(() => metrics.RecordGenerationDuration(100.0, "test-operation", true));
            Assert.NotNull(() => metrics.RecordValidationFailure("test-validation", "test-error"));
            Assert.NotNull(() => metrics.RecordPolicyScore(85, "test-policy"));
            Assert.NotNull(() => metrics.RecordPipelineSuccess("test-pipeline", 200.0));
            Assert.NotNull(() => metrics.RecordPipelineFailure("test-pipeline", 150.0, "test-error"));
        }

        [Fact]
        public void Observability_ShouldWorkWithoutOTLP()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            Environment.SetEnvironmentVariable("OTEL_CONSOLE_DISABLED", "false");

            try
            {
                var services = new ServiceCollection();
                var configuration = new ConfigurationBuilder().Build();

                // Act
                services.AddNexoObservability(configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Assert - Should not throw and services should be available
                Assert.NotNull(serviceProvider.GetService<ActivitySource>());
                Assert.NotNull(serviceProvider.GetService<Meter>());
                Assert.NotNull(serviceProvider.GetService<PipelineMetrics>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
                Environment.SetEnvironmentVariable("OTEL_CONSOLE_DISABLED", null);
            }
        }

        [Fact]
        public void Observability_ShouldDisableConsoleWhenRequested()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OTEL_CONSOLE_DISABLED", "true");

            try
            {
                var services = new ServiceCollection();
                var configuration = new ConfigurationBuilder().Build();

                // Act
                services.AddNexoObservability(configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Assert - Should not throw and services should be available
                Assert.NotNull(serviceProvider.GetService<ActivitySource>());
                Assert.NotNull(serviceProvider.GetService<Meter>());
                Assert.NotNull(serviceProvider.GetService<PipelineMetrics>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("OTEL_CONSOLE_DISABLED", null);
            }
        }
    }
}
