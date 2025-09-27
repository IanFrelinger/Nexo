using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Interfaces;

namespace Nexo.Feature.Platform.Tests.Services.NativeAPIIntegration
{
    /// <summary>
    /// Tests for Native API Integration interfaces.
    /// </summary>
    public partial class InterfaceTests
    {
        private readonly NativeAPIIntegration _integration;
        private readonly Mock<ILogger<NativeAPIIntegration>> _mockLogger;

        public InterfaceTests(NativeAPIIntegration integration, Mock<ILogger<NativeAPIIntegration>> mockLogger)
        {
            _integration = integration ?? throw new ArgumentNullException(nameof(integration));
            _mockLogger = mockLogger ?? throw new ArgumentNullException(nameof(mockLogger));
        }

        [Fact]
        public void INativeAPIIntegration_Interface_IsDefined()
        {
            // Arrange & Act
            var integration = _integration as INativeAPIIntegration;

            // Assert
            Assert.NotNull(integration);
        }

        [Fact]
        public void INativeAPIHandler_Interface_IsDefined()
        {
            // Arrange & Act
            var handler = new Mock<INativeAPIHandler>();

            // Assert
            Assert.NotNull(handler.Object);
        }

        [Fact]
        public void INativeAPIIntegration_Interface_HasRequiredMethods()
        {
            // Arrange
            var integration = _integration as INativeAPIIntegration;

            // Act & Assert
            Assert.NotNull(integration);
            Assert.True(typeof(INativeAPIIntegration).GetMethod("InitializeAsync") != null);
            Assert.True(typeof(INativeAPIIntegration).GetMethod("IntegrateWithNativeAPIAsync") != null);
            Assert.True(typeof(INativeAPIIntegration).GetMethod("GetAvailableAPIsAsync") != null);
            Assert.True(typeof(INativeAPIIntegration).GetMethod("GetAPICapabilitiesAsync") != null);
        }

        [Fact]
        public void INativeAPIHandler_Interface_HasRequiredMethods()
        {
            // Arrange
            var handler = new Mock<INativeAPIHandler>();

            // Act & Assert
            Assert.NotNull(handler.Object);
            Assert.True(typeof(INativeAPIHandler).GetMethod("HandleAPICallAsync") != null);
            Assert.True(typeof(INativeAPIHandler).GetMethod("ValidateAPICallAsync") != null);
            Assert.True(typeof(INativeAPIHandler).GetMethod("ProcessAPICallAsync") != null);
        }

        [Fact]
        public void INativeAPIIntegration_Interface_IsAsync()
        {
            // Arrange
            var integration = _integration as INativeAPIIntegration;

            // Act & Assert
            Assert.NotNull(integration);
            var methods = typeof(INativeAPIIntegration).GetMethods();
            foreach (var method in methods)
            {
                if (method.Name.EndsWith("Async"))
                {
                    Assert.True(method.ReturnType.IsGenericType && 
                               method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
                }
            }
        }

        [Fact]
        public void INativeAPIHandler_Interface_IsAsync()
        {
            // Arrange
            var handler = new Mock<INativeAPIHandler>();

            // Act & Assert
            Assert.NotNull(handler.Object);
            var methods = typeof(INativeAPIHandler).GetMethods();
            foreach (var method in methods)
            {
                if (method.Name.EndsWith("Async"))
                {
                    Assert.True(method.ReturnType.IsGenericType && 
                               method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
                }
            }
        }

        [Fact]
        public void INativeAPIIntegration_Interface_HasCorrectNamespace()
        {
            // Arrange & Act
            var interfaceType = typeof(INativeAPIIntegration);

            // Assert
            Assert.Equal("Nexo.Feature.Platform.Interfaces", interfaceType.Namespace);
        }

        [Fact]
        public void INativeAPIHandler_Interface_HasCorrectNamespace()
        {
            // Arrange & Act
            var interfaceType = typeof(INativeAPIHandler);

            // Assert
            Assert.Equal("Nexo.Feature.Platform.Interfaces", interfaceType.Namespace);
        }

        [Fact]
        public void INativeAPIIntegration_Interface_IsPublic()
        {
            // Arrange & Act
            var interfaceType = typeof(INativeAPIIntegration);

            // Assert
            Assert.True(interfaceType.IsPublic);
        }

        [Fact]
        public void INativeAPIHandler_Interface_IsPublic()
        {
            // Arrange & Act
            var interfaceType = typeof(INativeAPIHandler);

            // Assert
            Assert.True(interfaceType.IsPublic);
        }
    }
}
