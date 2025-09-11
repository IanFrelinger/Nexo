using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Xunit;

namespace Nexo.Core.Application.Tests.Interfaces.AI
{
    /// <summary>
    /// Tests for consolidated AI interfaces following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIInterfacesTests
    {
        [Fact]
        public void IAIEngine_ShouldHaveCorrectProperties()
        {
            // Arrange
            var engineType = typeof(IAIEngine);

            // Assert
            engineType.GetProperty("Id").Should().NotBeNull();
            engineType.GetProperty("Name").Should().NotBeNull();
            engineType.GetProperty("EngineType").Should().NotBeNull();
            engineType.GetProperty("Version").Should().NotBeNull();
            engineType.GetProperty("IsAvailable").Should().NotBeNull();
            engineType.GetProperty("IsInitialized").Should().NotBeNull();
            engineType.GetProperty("Capabilities").Should().NotBeNull();
            engineType.GetProperty("SupportedLanguages").Should().NotBeNull();
            engineType.GetProperty("Requirements").Should().NotBeNull();
            engineType.GetProperty("Performance").Should().NotBeNull();
            engineType.GetProperty("Environment").Should().NotBeNull();
        }

        [Fact]
        public void IAIEngine_ShouldHaveCorrectMethods()
        {
            // Arrange
            var engineType = typeof(IAIEngine);

            // Assert
            engineType.GetMethod("InitializeAsync").Should().NotBeNull();
            engineType.GetMethod("ShutdownAsync").Should().NotBeNull();
            engineType.GetMethod("GetInfoAsync").Should().NotBeNull();
            engineType.GetMethod("IsHealthyAsync").Should().NotBeNull();
            engineType.GetMethod("GetCapabilitiesAsync").Should().NotBeNull();
            engineType.GetMethod("GetSupportedLanguagesAsync").Should().NotBeNull();
            engineType.GetMethod("GetRequirementsAsync").Should().NotBeNull();
            engineType.GetMethod("GetPerformanceAsync").Should().NotBeNull();
            engineType.GetMethod("GetEnvironmentAsync").Should().NotBeNull();
        }

        [Fact]
        public void IAIProvider_ShouldHaveCorrectProperties()
        {
            // Arrange
            var providerType = typeof(IAIProvider);

            // Assert
            providerType.GetProperty("Id").Should().NotBeNull();
            providerType.GetProperty("Name").Should().NotBeNull();
            providerType.GetProperty("ProviderType").Should().NotBeNull();
            providerType.GetProperty("EngineType").Should().NotBeNull();
            providerType.GetProperty("Configuration").Should().NotBeNull();
            providerType.GetProperty("AvailableEngines").Should().NotBeNull();
            providerType.GetProperty("IsAvailable").Should().NotBeNull();
            providerType.GetProperty("IsInitialized").Should().NotBeNull();
        }

        [Fact]
        public void IAIProvider_ShouldHaveCorrectMethods()
        {
            // Arrange
            var providerType = typeof(IAIProvider);

            // Assert
            providerType.GetMethod("InitializeAsync").Should().NotBeNull();
            providerType.GetMethod("ShutdownAsync").Should().NotBeNull();
            providerType.GetMethod("GetInfoAsync").Should().NotBeNull();
            providerType.GetMethod("IsHealthyAsync").Should().NotBeNull();
            providerType.GetMethod("GetConfigurationAsync").Should().NotBeNull();
            providerType.GetMethod("GetAvailableEnginesAsync").Should().NotBeNull();
            providerType.GetMethod("GetEngineAsync", new[] { typeof(string) }).Should().NotBeNull();
            providerType.GetMethod("SupportsEngineTypeAsync", new[] { typeof(AIEngineType) }).Should().NotBeNull();
            providerType.GetMethod("CreateEngineAsync", new[] { typeof(string) }).Should().NotBeNull();
            providerType.GetMethod("DisposeEngineAsync", new[] { typeof(string) }).Should().NotBeNull();
        }

        [Fact]
        public void IAIService_ShouldHaveCorrectProperties()
        {
            // Arrange
            var serviceType = typeof(IAIService);

            // Assert
            serviceType.GetProperty("Id").Should().NotBeNull();
            serviceType.GetProperty("Name").Should().NotBeNull();
            serviceType.GetProperty("OperationType").Should().NotBeNull();
            serviceType.GetProperty("Configuration").Should().NotBeNull();
            serviceType.GetProperty("IsAvailable").Should().NotBeNull();
            serviceType.GetProperty("IsInitialized").Should().NotBeNull();
        }

        [Fact]
        public void IAIService_ShouldHaveCorrectMethods()
        {
            // Arrange
            var serviceType = typeof(IAIService);

            // Assert
            serviceType.GetMethod("InitializeAsync").Should().NotBeNull();
            serviceType.GetMethod("ShutdownAsync").Should().NotBeNull();
            serviceType.GetMethod("GetInfoAsync").Should().NotBeNull();
            serviceType.GetMethod("IsHealthyAsync").Should().NotBeNull();
            serviceType.GetMethod("GetConfigurationAsync").Should().NotBeNull();
            serviceType.GetMethod("ExecuteAsync", new[] { typeof(BaseRequest) }).Should().NotBeNull();
            serviceType.GetMethod("ExecuteAsync", new[] { typeof(BaseRequest) }).Should().NotBeNull();
        }

        [Fact]
        public void AIProviderInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            var infoType = typeof(AIProviderInfo);

            // Assert
            infoType.GetProperty("Id").Should().NotBeNull();
            infoType.GetProperty("Name").Should().NotBeNull();
            infoType.GetProperty("ProviderType").Should().NotBeNull();
            infoType.GetProperty("EngineType").Should().NotBeNull();
            infoType.GetProperty("Configuration").Should().NotBeNull();
            infoType.GetProperty("AvailableEngines").Should().NotBeNull();
            infoType.GetProperty("IsAvailable").Should().NotBeNull();
            infoType.GetProperty("IsInitialized").Should().NotBeNull();
            infoType.GetProperty("CreatedAt").Should().NotBeNull();
            infoType.GetProperty("UpdatedAt").Should().NotBeNull();
        }

        [Fact]
        public void AIServiceInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            var infoType = typeof(AIServiceInfo);

            // Assert
            infoType.GetProperty("Id").Should().NotBeNull();
            infoType.GetProperty("Name").Should().NotBeNull();
            infoType.GetProperty("OperationType").Should().NotBeNull();
            infoType.GetProperty("Configuration").Should().NotBeNull();
            infoType.GetProperty("IsAvailable").Should().NotBeNull();
            infoType.GetProperty("IsInitialized").Should().NotBeNull();
            infoType.GetProperty("CreatedAt").Should().NotBeNull();
            infoType.GetProperty("UpdatedAt").Should().NotBeNull();
        }

        [Fact]
        public void IAIEngine_ShouldBeAsync()
        {
            // Arrange
            var engineType = typeof(IAIEngine);

            // Assert
            engineType.GetMethod("InitializeAsync").ReturnType.Should().Be(typeof(Task<bool>));
            engineType.GetMethod("ShutdownAsync").ReturnType.Should().Be(typeof(Task<bool>));
            engineType.GetMethod("GetInfoAsync").ReturnType.Should().Be(typeof(Task<AIEngineInfo>));
            engineType.GetMethod("IsHealthyAsync").ReturnType.Should().Be(typeof(Task<bool>));
            engineType.GetMethod("GetCapabilitiesAsync").ReturnType.Should().Be(typeof(Task<Dictionary<string, object>>));
            engineType.GetMethod("GetSupportedLanguagesAsync").ReturnType.Should().Be(typeof(Task<List<string>>));
            engineType.GetMethod("GetRequirementsAsync").ReturnType.Should().Be(typeof(Task<AIRequirements>));
            engineType.GetMethod("GetPerformanceAsync").ReturnType.Should().Be(typeof(Task<PerformanceEstimate>));
            engineType.GetMethod("GetEnvironmentAsync").ReturnType.Should().Be(typeof(Task<EnvironmentProfile>));
        }

        [Fact]
        public void IAIProvider_ShouldBeAsync()
        {
            // Arrange
            var providerType = typeof(IAIProvider);

            // Assert
            providerType.GetMethod("InitializeAsync").ReturnType.Should().Be(typeof(Task<bool>));
            providerType.GetMethod("ShutdownAsync").ReturnType.Should().Be(typeof(Task<bool>));
            providerType.GetMethod("GetInfoAsync").ReturnType.Should().Be(typeof(Task<AIProviderInfo>));
            providerType.GetMethod("IsHealthyAsync").ReturnType.Should().Be(typeof(Task<bool>));
            providerType.GetMethod("GetConfigurationAsync").ReturnType.Should().Be(typeof(Task<Dictionary<string, object>>));
            providerType.GetMethod("GetAvailableEnginesAsync").ReturnType.Should().Be(typeof(Task<List<AIEngineInfo>>));
            providerType.GetMethod("GetEngineAsync", new[] { typeof(string) }).ReturnType.Should().Be(typeof(Task<AIEngineInfo>));
            providerType.GetMethod("SupportsEngineTypeAsync", new[] { typeof(AIEngineType) }).ReturnType.Should().Be(typeof(Task<bool>));
            providerType.GetMethod("CreateEngineAsync", new[] { typeof(string) }).ReturnType.Should().Be(typeof(Task<AIEngine>));
            providerType.GetMethod("DisposeEngineAsync", new[] { typeof(string) }).ReturnType.Should().Be(typeof(Task<bool>));
        }

        [Fact]
        public void IAIService_ShouldBeAsync()
        {
            // Arrange
            var serviceType = typeof(IAIService);

            // Assert
            serviceType.GetMethod("InitializeAsync").ReturnType.Should().Be(typeof(Task<bool>));
            serviceType.GetMethod("ShutdownAsync").ReturnType.Should().Be(typeof(Task<bool>));
            serviceType.GetMethod("GetInfoAsync").ReturnType.Should().Be(typeof(Task<AIServiceInfo>));
            serviceType.GetMethod("IsHealthyAsync").ReturnType.Should().Be(typeof(Task<bool>));
            serviceType.GetMethod("GetConfigurationAsync").ReturnType.Should().Be(typeof(Task<Dictionary<string, object>>));
            serviceType.GetMethod("ExecuteAsync", new[] { typeof(BaseRequest) }).ReturnType.Should().Be(typeof(Task<BaseResult>));
            serviceType.GetMethod("ExecuteAsync", new[] { typeof(BaseRequest) }).ReturnType.Should().Be(typeof(Task<BaseResult>));
        }
    }
}
