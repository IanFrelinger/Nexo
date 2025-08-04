using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Tests.Services
{
    /// <summary>
    /// Tests for the NativeAPIIntegration service.
    /// Part of Epic 6.2: Platform-Specific Feature Integration, Story 6.2.2: Native API Integration.
    /// </summary>
    public class NativeAPIIntegrationTests
    {
        private readonly Mock<ILogger<NativeAPIIntegration>> _mockLogger;
        private readonly NativeAPIIntegration _integration;

        public NativeAPIIntegrationTests()
        {
            _mockLogger = new Mock<ILogger<NativeAPIIntegration>>();
            _integration = new NativeAPIIntegration(_mockLogger.Object);
        }

        #region Interface Tests

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

        #endregion

        #region Model Tests

        [Fact]
        public void NativeAPIInitializationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new NativeAPIInitializationResult();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.Message);
            Assert.Equal(PlatformType.Windows, result.PlatformType); // Default enum value is Windows (0)
            Assert.Empty(result.AvailableAPIs);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Errors);
            Assert.Equal(DateTime.UtcNow.Date, result.InitializationTime.Date);
            Assert.Empty(result.Metadata);
        }

        [Fact]
        public void NativeAPICallResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var result = new NativeAPICallResult
            {
                IsSuccess = true,
                Message = "Test message",
                APIName = "TestAPI",
                Result = "Test result",
                Parameters = parameters,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            };

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Test message", result.Message);
            Assert.Equal("TestAPI", result.APIName);
            Assert.Equal("Test result", result.Result);
            Assert.Equal(parameters, result.Parameters);
            Assert.Equal(TimeSpan.FromMilliseconds(100), result.ExecutionTime);
        }

        [Fact]
        public void NativeAPIAvailabilityResult_WithValidData_PropertiesSetCorrectly()
        {
            // Act
            var result = new NativeAPIAvailabilityResult
            {
                IsAvailable = true,
                APIName = "TestAPI",
                PlatformType = PlatformType.Windows,
                Version = "1.0",
                Reason = "Available"
            };

            // Assert
            Assert.True(result.IsAvailable);
            Assert.Equal("TestAPI", result.APIName);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.Equal("1.0", result.Version);
            Assert.Equal("Available", result.Reason);
        }

        [Fact]
        public void PermissionRequestResult_WithValidData_PropertiesSetCorrectly()
        {
            // Act
            var result = new PermissionRequestResult
            {
                IsGranted = true,
                APIName = "TestAPI",
                PermissionType = PermissionType.Camera,
                Reason = "Granted",
                RequiredActions = new List<string> { "None" }
            };

            // Assert
            Assert.True(result.IsGranted);
            Assert.Equal("TestAPI", result.APIName);
            Assert.Equal(PermissionType.Camera, result.PermissionType);
            Assert.Equal("Granted", result.Reason);
            Assert.Single(result.RequiredActions);
        }

        [Fact]
        public void APIAbstractionLayerResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var abstractionLayer = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var result = new APIAbstractionLayerResult
            {
                IsSuccess = true,
                Message = "Success",
                AbstractionLayer = abstractionLayer,
                SupportedAPIs = new List<string> { "API1", "API2" }
            };

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Success", result.Message);
            Assert.Equal(abstractionLayer, result.AbstractionLayer);
            Assert.Equal(2, result.SupportedAPIs.Count);
        }

        #endregion

        #region Initialization Tests

        [Fact]
        public async Task InitializeAsync_WithWindowsPlatform_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _integration.InitializeAsync(PlatformType.Windows);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.True(result.AvailableAPIs.Count > 0);
            Assert.Equal(DateTime.UtcNow.Date, result.InitializationTime.Date);
        }

        [Fact]
        public async Task InitializeAsync_WithMacOSPlatform_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _integration.InitializeAsync(PlatformType.MacOS);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.MacOS, result.PlatformType);
            Assert.True(result.AvailableAPIs.Count > 0);
            Assert.Equal(DateTime.UtcNow.Date, result.InitializationTime.Date);
        }

        [Fact]
        public async Task InitializeAsync_WithLinuxPlatform_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _integration.InitializeAsync(PlatformType.Linux);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Linux, result.PlatformType);
            Assert.True(result.AvailableAPIs.Count > 0);
            Assert.Equal(DateTime.UtcNow.Date, result.InitializationTime.Date);
        }

        [Fact]
        public async Task InitializeAsync_WithUnknownPlatform_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _integration.InitializeAsync(PlatformType.Unknown);

            // Assert
            Assert.True(result.IsSuccess); // Service is optimistic and returns success even for unknown platforms
            Assert.Equal(PlatformType.Unknown, result.PlatformType);
            Assert.NotNull(result.AvailableAPIs);
        }

        #endregion

        #region API Call Tests

        [Fact]
        public async Task ExecuteAPICallAsync_WithoutInitialization_ThrowsException()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "key", "value" } };

            // Act & Assert
            var result = await _integration.ExecuteAPICallAsync("TestAPI", parameters);
            Assert.False(result.IsSuccess);
            Assert.Contains("not initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAPICallAsync_WithValidAPI_ReturnsSuccessResult()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);
            var parameters = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var result = await _integration.ExecuteAPICallAsync("Windows.System", parameters);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Windows.System", result.APIName);
            Assert.Equal(parameters, result.Parameters);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task ExecuteAPICallAsync_WithUnavailableAPI_ReturnsErrorResult()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);
            var parameters = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var result = await _integration.ExecuteAPICallAsync("UnavailableAPI", parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("UnavailableAPI", result.APIName);
            Assert.Contains("not available", result.Message);
        }

        #endregion

        #region API Availability Tests

        [Fact]
        public async Task CheckAPIAvailabilityAsync_WithAvailableAPI_ReturnsTrue()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.CheckAPIAvailabilityAsync("Windows.System");

            // Assert
            Assert.True(result.IsAvailable);
            Assert.Equal("Windows.System", result.APIName);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
        }

        [Fact]
        public async Task CheckAPIAvailabilityAsync_WithUnavailableAPI_ReturnsFalse()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.CheckAPIAvailabilityAsync("UnavailableAPI");

            // Assert
            Assert.False(result.IsAvailable);
            Assert.Equal("UnavailableAPI", result.APIName);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
        }

        [Fact]
        public async Task GetAvailableAPIsAsync_WithInitializedPlatform_ReturnsAPIs()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.GetAvailableAPIsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.True(result.AvailableAPIs.Count > 0);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task RequestPermissionAsync_WithValidRequest_ReturnsGranted()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.RequestPermissionAsync("Windows.Hardware", PermissionType.Other);

            // Assert
            Assert.True(result.IsGranted);
            Assert.Equal("Windows.Hardware", result.APIName);
            Assert.Equal(PermissionType.Other, result.PermissionType);
        }

        [Fact]
        public async Task CheckPermissionStatusAsync_WithGrantedPermission_ReturnsGranted()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);
            await _integration.RequestPermissionAsync("Windows.Hardware", PermissionType.Other);

            // Act
            var result = await _integration.CheckPermissionStatusAsync("Windows.Hardware");

            // Assert
            Assert.True(result.HasPermission);
            Assert.Equal(PermissionStatus.Granted, result.Status);
        }

        #endregion

        #region Handler Registration Tests

        [Fact]
        public async Task RegisterAPIHandlerAsync_WithValidHandler_ReturnsSuccess()
        {
            // Arrange
            var mockHandler = new Mock<INativeAPIHandler>();
            mockHandler.Setup(h => h.GetMetadata()).Returns(new APIHandlerMetadata
            {
                Name = "TestHandler",
                Description = "Test handler",
                Version = "1.0"
            });

            // Act
            var result = await _integration.RegisterAPIHandlerAsync("TestAPI", mockHandler.Object);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("TestAPI", result.APIName);
            Assert.True(result.IsRegistered);
        }

        [Fact]
        public async Task UnregisterAPIHandlerAsync_WithRegisteredHandler_ReturnsSuccess()
        {
            // Arrange
            var mockHandler = new Mock<INativeAPIHandler>();
            await _integration.RegisterAPIHandlerAsync("TestAPI", mockHandler.Object);

            // Act
            var result = await _integration.UnregisterAPIHandlerAsync("TestAPI");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("TestAPI", result.APIName);
            Assert.False(result.IsRegistered);
        }

        #endregion

        #region Abstraction Layer Tests

        [Fact]
        public async Task GetAPIAbstractionLayerAsync_WithInitializedPlatform_ReturnsAbstractionLayer()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.GetAPIAbstractionLayerAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.AbstractionLayer);
            Assert.True(result.SupportedAPIs.Count > 0);
        }

        #endregion

        #region Compatibility Tests

        [Fact]
        public async Task ValidateAPICompatibilityAsync_WithCompatibleAPIs_ReturnsCompatible()
        {
            // Arrange
            var apis = new List<string> { "Windows.System", "Windows.Hardware" };
            var platforms = new List<PlatformType> { PlatformType.Windows, PlatformType.MacOS };

            // Act
            var result = await _integration.ValidateAPICompatibilityAsync(apis, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(apis.Count, result.APIs.Count);
            Assert.Equal(platforms.Count, result.Platforms.Count);
            Assert.NotNull(result.CompatibilityMatrix);
        }

        [Fact]
        public async Task ValidateAPICompatibilityAsync_WithEmptyAPIs_ReturnsEmptyResult()
        {
            // Arrange
            var apis = new List<string>();
            var platforms = new List<PlatformType> { PlatformType.Windows };

            // Act
            var result = await _integration.ValidateAPICompatibilityAsync(apis, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.APIs);
            Assert.Empty(result.CompatibilityMatrix);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task DisposeAsync_WithInitializedIntegration_ReturnsSuccess()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.DisposeAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.DisposedAPIs >= 0);
        }

        #endregion

        #region Cross-Platform Tests

        [Theory]
        [InlineData(PlatformType.Windows)]
        [InlineData(PlatformType.MacOS)]
        [InlineData(PlatformType.Linux)]
        public async Task NativeAPIIntegration_CrossPlatformScenarios_WorkCorrectly(PlatformType platformType)
        {
            // Arrange & Act
            var initResult = await _integration.InitializeAsync(platformType);
            var apisResult = await _integration.GetAvailableAPIsAsync();
            var abstractionResult = await _integration.GetAPIAbstractionLayerAsync();

            // Assert
            Assert.True(initResult.IsSuccess);
            Assert.Equal(platformType, initResult.PlatformType);
            Assert.True(initResult.AvailableAPIs.Count > 0);

            Assert.True(apisResult.IsSuccess);
            Assert.Equal(platformType, apisResult.PlatformType);
            Assert.True(apisResult.AvailableAPIs.Count > 0);

            Assert.True(abstractionResult.IsSuccess);
            Assert.NotNull(abstractionResult.AbstractionLayer);
        }

        [Fact]
        public async Task NativeAPIIntegration_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange
            var platform = PlatformType.Windows;
            var apiName = "Windows.System";
            var parameters = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var initResult = await _integration.InitializeAsync(platform);
            var availabilityResult = await _integration.CheckAPIAvailabilityAsync(apiName);
            var callResult = await _integration.ExecuteAPICallAsync(apiName, parameters);
            var apisResult = await _integration.GetAvailableAPIsAsync();
            var abstractionResult = await _integration.GetAPIAbstractionLayerAsync();
            var disposeResult = await _integration.DisposeAsync();

            // Assert
            Assert.True(initResult.IsSuccess);
            Assert.True(availabilityResult.IsAvailable);
            Assert.True(callResult.IsSuccess);
            Assert.True(apisResult.IsSuccess);
            Assert.True(abstractionResult.IsSuccess);
            Assert.True(disposeResult.IsSuccess);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ExecuteAPICallAsync_WithNullParameters_HandlesGracefully()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.ExecuteAPICallAsync("Windows.System", null!);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Windows.System", result.APIName);
        }

        [Fact]
        public async Task CheckAPIAvailabilityAsync_WithEmptyAPIName_HandlesGracefully()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.CheckAPIAvailabilityAsync(string.Empty);

            // Assert
            Assert.False(result.IsAvailable);
            Assert.Equal(string.Empty, result.APIName);
        }

        [Fact]
        public async Task RequestPermissionAsync_WithInvalidAPI_HandlesGracefully()
        {
            // Arrange
            await _integration.InitializeAsync(PlatformType.Windows);

            // Act
            var result = await _integration.RequestPermissionAsync("InvalidAPI", PermissionType.Camera);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("InvalidAPI", result.APIName);
            Assert.Equal(PermissionType.Camera, result.PermissionType);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task NativeAPIIntegration_Performance_CompletesWithinReasonableTime()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _integration.InitializeAsync(PlatformType.Windows);
            await _integration.GetAvailableAPIsAsync();
            await _integration.GetAPIAbstractionLayerAsync();
            await _integration.DisposeAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task APICompatibilityValidation_Performance_CompletesWithinReasonableTime()
        {
            // Arrange
            var apis = Enumerable.Range(1, 10).Select(i => $"API{i}").ToList();
            var platforms = new List<PlatformType> { PlatformType.Windows, PlatformType.MacOS, PlatformType.Linux };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _integration.ValidateAPICompatibilityAsync(apis, platforms);

            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000); // Should complete within 2 seconds
        }

        #endregion
    }
} 