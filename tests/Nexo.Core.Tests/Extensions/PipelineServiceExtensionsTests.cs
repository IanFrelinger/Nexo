using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Contracts;
using Nexo.Core.Extensions;
using Nexo.Core.Pipeline;
using Xunit;

namespace Nexo.Core.Tests.Extensions
{
    public class PipelineServiceExtensionsTests
    {
        [Fact]
        public void AddPipelineServices_ShouldRegisterGenericPipeline()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPipelineServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var pipelineType = typeof(ExtensionPipeline<,>);
            
            // Should be able to resolve the generic type
            var pipeline = serviceProvider.GetService(pipelineType);
            pipeline.Should().NotBeNull();
        }

        [Fact]
        public void AddPipelineServices_WithSpecificTypes_ShouldRegisterSpecificPipeline()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPipelineServices<TestRequest, TestArtifact>();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var pipeline = serviceProvider.GetService<ExtensionPipeline<TestRequest, TestArtifact>>();
            pipeline.Should().NotBeNull();
        }

        [Fact]
        public void AddPipelineServices_WithNoGates_ShouldRegisterDefaultGates()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPipelineServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var gates = serviceProvider.GetService<IEnumerable<ICompilationGate>>();
            gates.Should().NotBeNull();
            gates.Should().HaveCount(1);
            gates!.First().Should().BeOfType<DefaultCompilationGate>();
        }

        [Fact]
        public void AddPipelineServices_WithNoPolicies_ShouldRegisterDefaultPolicies()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPipelineServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var policies = serviceProvider.GetService<IEnumerable<IPolicyGate<object>>>();
            policies.Should().NotBeNull();
            policies.Should().HaveCount(1);
            policies!.First().Should().BeOfType<DefaultPolicyGate<object>>();
        }

        [Fact]
        public void AddPipelineServices_WithExistingGates_ShouldUseExistingGates()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockGate = new Mock<ICompilationGate>();
            services.AddSingleton<ICompilationGate>(mockGate.Object);

            // Act
            services.AddPipelineServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var gates = serviceProvider.GetService<IEnumerable<ICompilationGate>>();
            gates.Should().NotBeNull();
            gates.Should().HaveCount(1);
            gates!.First().Should().Be(mockGate.Object);
        }

        [Fact]
        public void AddPipelineServices_WithExistingPolicies_ShouldUseExistingPolicies()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockPolicy = new Mock<IPolicyGate<object>>();
            services.AddSingleton<IPolicyGate<object>>(mockPolicy.Object);

            // Act
            services.AddPipelineServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var policies = serviceProvider.GetService<IEnumerable<IPolicyGate<object>>>();
            policies.Should().NotBeNull();
            policies.Should().HaveCount(1);
            policies!.First().Should().Be(mockPolicy.Object);
        }
    }

    // Test types for the service extension tests
    public class TestRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class TestArtifact
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
