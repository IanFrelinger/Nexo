using System;
using System.Collections.Generic;
using Xunit;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests.Services.NativeAPIIntegration
{
    /// <summary>
    /// Tests for Native API Integration models.
    /// </summary>
    public class ModelTests
    {
        [Fact]
        public void NativeAPIInitializationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new NativeAPIInitializationResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Empty(result.Message);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Capabilities);
            Assert.Equal(DateTime.MinValue, result.InitializationTime);
        }

        [Fact]
        public void NativeAPIInitializationResult_WithValues_InitializesCorrectly()
        {
            // Arrange
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "TestCapability", IsSupported = true }
            };

            // Act
            var result = new NativeAPIInitializationResult
            {
                Success = true,
                Message = "Test message",
                Errors = new List<string> { "Test error" },
                Warnings = new List<string> { "Test warning" },
                Capabilities = capabilities,
                InitializationTime = DateTime.UtcNow
            };

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Test message", result.Message);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Capabilities);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }

        [Fact]
        public void NativeAPICapability_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var capability = new NativeAPICapability();

            // Assert
            Assert.NotNull(capability);
            Assert.Empty(capability.Name);
            Assert.False(capability.IsSupported);
            Assert.Empty(capability.Description);
            Assert.Empty(capability.Version);
            Assert.Empty(capability.Requirements);
            Assert.Empty(capability.Limitations);
        }

        [Fact]
        public void NativeAPICapability_WithValues_InitializesCorrectly()
        {
            // Arrange
            var requirements = new List<string> { "Requirement1", "Requirement2" };
            var limitations = new List<string> { "Limitation1", "Limitation2" };

            // Act
            var capability = new NativeAPICapability
            {
                Name = "TestCapability",
                IsSupported = true,
                Description = "Test description",
                Version = "1.0.0",
                Requirements = requirements,
                Limitations = limitations
            };

            // Assert
            Assert.NotNull(capability);
            Assert.Equal("TestCapability", capability.Name);
            Assert.True(capability.IsSupported);
            Assert.Equal("Test description", capability.Description);
            Assert.Equal("1.0.0", capability.Version);
            Assert.Equal(2, capability.Requirements.Count);
            Assert.Equal(2, capability.Limitations.Count);
        }

        [Fact]
        public void NativeAPIIntegrationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new NativeAPIIntegrationResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Empty(result.Message);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.IntegratedAPIs);
            Assert.Equal(DateTime.MinValue, result.IntegrationTime);
        }

        [Fact]
        public void NativeAPIIntegrationResult_WithValues_InitializesCorrectly()
        {
            // Arrange
            var integratedAPIs = new List<string> { "API1", "API2" };

            // Act
            var result = new NativeAPIIntegrationResult
            {
                Success = true,
                Message = "Test message",
                Errors = new List<string> { "Test error" },
                Warnings = new List<string> { "Test warning" },
                IntegratedAPIs = integratedAPIs,
                IntegrationTime = DateTime.UtcNow
            };

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Test message", result.Message);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Equal(2, result.IntegratedAPIs.Count);
            Assert.True(result.IntegrationTime > DateTime.MinValue);
        }

        [Fact]
        public void NativeAPICapability_Properties_AreSettable()
        {
            // Arrange
            var capability = new NativeAPICapability();

            // Act
            capability.Name = "TestName";
            capability.IsSupported = true;
            capability.Description = "TestDescription";
            capability.Version = "TestVersion";
            capability.Requirements = new List<string> { "Req1" };
            capability.Limitations = new List<string> { "Lim1" };

            // Assert
            Assert.Equal("TestName", capability.Name);
            Assert.True(capability.IsSupported);
            Assert.Equal("TestDescription", capability.Description);
            Assert.Equal("TestVersion", capability.Version);
            Assert.Single(capability.Requirements);
            Assert.Single(capability.Limitations);
        }

        [Fact]
        public void NativeAPIInitializationResult_Properties_AreSettable()
        {
            // Arrange
            var result = new NativeAPIInitializationResult();

            // Act
            result.Success = true;
            result.Message = "TestMessage";
            result.Errors = new List<string> { "Error1" };
            result.Warnings = new List<string> { "Warning1" };
            result.Capabilities = new List<NativeAPICapability>();
            result.InitializationTime = DateTime.UtcNow;

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestMessage", result.Message);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.NotNull(result.Capabilities);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }

        [Fact]
        public void NativeAPIIntegrationResult_Properties_AreSettable()
        {
            // Arrange
            var result = new NativeAPIIntegrationResult();

            // Act
            result.Success = true;
            result.Message = "TestMessage";
            result.Errors = new List<string> { "Error1" };
            result.Warnings = new List<string> { "Warning1" };
            result.IntegratedAPIs = new List<string> { "API1" };
            result.IntegrationTime = DateTime.UtcNow;

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestMessage", result.Message);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.IntegratedAPIs);
            Assert.True(result.IntegrationTime > DateTime.MinValue);
        }
    }
}
