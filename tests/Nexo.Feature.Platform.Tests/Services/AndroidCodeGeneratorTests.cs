using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Feature.Platform.Tests.Services
{
    /// <summary>
    /// Unit tests for the AndroidCodeGenerator service.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.2: Android Native Implementation.
    /// </summary>
    public class AndroidCodeGeneratorTests
    {
        private readonly Mock<ILogger<AndroidCodeGenerator>> _mockLogger;
        private readonly IAndroidCodeGenerator _androidCodeGenerator;

        public AndroidCodeGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<AndroidCodeGenerator>>();
            _androidCodeGenerator = new AndroidCodeGenerator(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var androidOptions = new AndroidGenerationOptions();

            // Act
            var result = await _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedCode);
            Assert.True(result.GenerationScore > 0);
            Assert.NotEmpty(result.GeneratedCode.ComposeFiles);
            Assert.NotEmpty(result.GeneratedCode.RoomFiles);
            Assert.NotEmpty(result.GeneratedCode.CoroutinesFiles);
            Assert.NotEmpty(result.GeneratedCode.AppliedUIPatterns);
            Assert.NotEmpty(result.GeneratedCode.AppliedOptimizations);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithNullApplicationLogic_ThrowsArgumentNullException()
        {
            // Arrange
            StandardizedApplicationLogic applicationLogic = null;
            var androidOptions = new AndroidGenerationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions));
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            AndroidGenerationOptions androidOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions));
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var androidOptions = new AndroidGenerationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task IntegrateRoomDatabaseAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var roomOptions = new RoomDatabaseOptions();

            // Act
            var result = await _androidCodeGenerator.IntegrateRoomDatabaseAsync(applicationLogic, roomOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedFiles);
            Assert.True(result.IntegrationScore > 0);
            Assert.Equal("Room database integration completed successfully", result.Message);
        }

        [Fact]
        public async Task CreateKotlinCoroutinesOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var coroutinesOptions = new KotlinCoroutinesOptions();

            // Act
            var result = await _androidCodeGenerator.CreateKotlinCoroutinesOptimizationAsync(applicationLogic, coroutinesOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedFiles);
            Assert.True(result.OptimizationScore > 0);
            Assert.Equal("Kotlin coroutines optimization completed successfully", result.Message);
        }

        [Fact]
        public async Task GenerateAndroidUIPatternsAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var uiPatternOptions = new AndroidUIPatternOptions();

            // Act
            var result = await _androidCodeGenerator.GenerateAndroidUIPatternsAsync(applicationLogic, uiPatternOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedPatterns);
            Assert.True(result.PatternScore > 0);
            Assert.Equal("Android UI patterns generation completed successfully", result.Message);
        }

        [Fact]
        public async Task CreateAndroidPerformanceOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var performanceOptions = new AndroidPerformanceOptions();

            // Act
            var result = await _androidCodeGenerator.CreateAndroidPerformanceOptimizationAsync(applicationLogic, performanceOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedOptimizations);
            Assert.True(result.PerformanceScore > 0);
            Assert.Equal("Android performance optimization completed successfully", result.Message);
        }

        [Fact]
        public async Task GenerateAndroidAppConfigurationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var configOptions = new AndroidAppConfigOptions();

            // Act
            var result = await _androidCodeGenerator.GenerateAndroidAppConfigurationAsync(applicationLogic, configOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedConfiguration);
            Assert.True(result.ConfigurationScore > 0);
            Assert.Equal("Android app configuration generation completed successfully", result.Message);
        }

        [Fact]
        public async Task ValidateAndroidCodeAsync_WithValidCode_ReturnsValidResult()
        {
            // Arrange
            var androidCode = CreateValidAndroidGeneratedCode();
            var validationOptions = new AndroidValidationOptions();

            // Act
            var result = await _androidCodeGenerator.ValidateAndroidCodeAsync(androidCode, validationOptions);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ValidationScore > 0);
            Assert.Equal("Android code validation passed", result.Message);
        }

        [Fact]
        public async Task ValidateAndroidCodeAsync_WithInvalidCode_ReturnsInvalidResult()
        {
            // Arrange
            var androidCode = CreateInvalidAndroidGeneratedCode();
            var validationOptions = new AndroidValidationOptions();

            // Act
            var result = await _androidCodeGenerator.ValidateAndroidCodeAsync(androidCode, validationOptions);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(0.0, result.ValidationScore);
            Assert.Equal("Android code validation failed", result.Message);
            Assert.NotEmpty(result.ValidationErrors);
        }

        [Fact]
        public void GetSupportedAndroidUIPatterns_ReturnsExpectedPatterns()
        {
            // Act
            var patterns = _androidCodeGenerator.GetSupportedAndroidUIPatterns().ToList();

            // Assert
            Assert.NotEmpty(patterns);
            Assert.Contains(patterns, p => p.Type == AndroidUIPatternType.Navigation);
            Assert.Contains(patterns, p => p.Type == AndroidUIPatternType.BottomNavigation);
            Assert.Contains(patterns, p => p.Type == AndroidUIPatternType.MaterialDesign);
        }

        [Fact]
        public void GetSupportedAndroidPerformanceOptimizations_ReturnsExpectedOptimizations()
        {
            // Act
            var optimizations = _androidCodeGenerator.GetSupportedAndroidPerformanceOptimizations().ToList();

            // Assert
            Assert.NotEmpty(optimizations);
            Assert.Contains(optimizations, o => o.Type == AndroidPerformanceType.Memory);
            Assert.Contains(optimizations, o => o.Type == AndroidPerformanceType.Battery);
        }

        [Fact]
        public void GetSupportedKotlinCoroutinesFeatures_ReturnsExpectedFeatures()
        {
            // Act
            var features = _androidCodeGenerator.GetSupportedKotlinCoroutinesFeatures().ToList();

            // Assert
            Assert.NotEmpty(features);
            Assert.Contains(features, f => f.Type == KotlinCoroutinesFeatureType.Launch);
            Assert.Contains(features, f => f.Type == KotlinCoroutinesFeatureType.Async);
            Assert.Contains(features, f => f.Type == KotlinCoroutinesFeatureType.Flow);
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithDisabledFeatures_GeneratesMinimalCode()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var androidOptions = new AndroidGenerationOptions
            {
                EnableJetpackCompose = false,
                EnableRoomDatabase = false,
                EnableKotlinCoroutines = false,
                EnablePerformanceOptimization = false
            };

            // Act
            var result = await _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.GeneratedCode.ComposeFiles);
            Assert.Empty(result.GeneratedCode.RoomFiles);
            Assert.Empty(result.GeneratedCode.CoroutinesFiles);
            Assert.Empty(result.GeneratedCode.AppliedOptimizations);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithEmptyApplicationLogic_GeneratesBasicCode()
        {
            // Arrange
            var applicationLogic = CreateEmptyApplicationLogic();
            var androidOptions = new AndroidGenerationOptions();

            // Act
            var result = await _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedCode.ComposeFiles);
            Assert.NotEmpty(result.GeneratedCode.RoomFiles);
            Assert.NotEmpty(result.GeneratedCode.CoroutinesFiles);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        // Helper methods
        private StandardizedApplicationLogic CreateValidApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>
                {
                    new ApplicationPattern
                    {
                        Name = "Repository",
                        Type = PatternType.Repository,
                        Implementation = "Repository pattern implementation"
                    },
                    new ApplicationPattern
                    {
                        Name = "UnitOfWork",
                        Type = PatternType.UnitOfWork,
                        Implementation = "Unit of Work pattern implementation"
                    }
                },
                SecurityPatterns = new List<SecurityPattern>
                {
                    new SecurityPattern
                    {
                        Name = "JWT",
                        Type = SecurityPatternType.JWT,
                        Implementation = "JWT authentication"
                    }
                },
                StateManagementPatterns = new List<StateManagementPattern>
                {
                    new StateManagementPattern
                    {
                        Name = "GlobalState",
                        Type = StateManagementType.GlobalState,
                        Implementation = "Global state management"
                    }
                },
                ApiContracts = new List<ApiContract>
                {
                    new ApiContract
                    {
                        Name = "UserAPI",
                        Method = Nexo.Feature.AI.Models.HttpMethod.GET,
                        Endpoint = "/api/users",
                        Parameters = new List<ApiParameter>(),
                        Response = new ApiResponse()
                    }
                },
                DataFlowPatterns = new List<DataFlowPattern>
                {
                    new DataFlowPattern
                    {
                        Name = "Unidirectional",
                        Type = DataFlowType.Unidirectional,
                        Implementation = "Unidirectional data flow"
                    }
                },
                CachingStrategies = new List<CachingStrategy>
                {
                    new CachingStrategy
                    {
                        Name = "MemoryCache",
                        Type = CachingStrategyType.MemoryCache,
                        Implementation = "In-memory caching"
                    }
                }
            };
        }

        private StandardizedApplicationLogic CreateEmptyApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>(),
                SecurityPatterns = new List<SecurityPattern>(),
                StateManagementPatterns = new List<StateManagementPattern>(),
                ApiContracts = new List<ApiContract>(),
                DataFlowPatterns = new List<DataFlowPattern>(),
                CachingStrategies = new List<CachingStrategy>()
            };
        }

        private AndroidGeneratedCode CreateValidAndroidGeneratedCode()
        {
            return new AndroidGeneratedCode
            {
                ComposeFiles = new List<ComposeFile>
                {
                    new ComposeFile
                    {
                        FileName = "MainScreen.kt",
                        Content = "package com.example.app\n\n@Composable\nfun MainScreen() { }",
                        ViewType = ComposeViewType.Screen
                    }
                },
                RoomFiles = new List<RoomFile>
                {
                    new RoomFile
                    {
                        FileName = "AppDatabase.kt",
                        Content = "package com.example.app\n\n@Database(entities = [], version = 1)\nabstract class AppDatabase : RoomDatabase() { }",
                        FileType = RoomFileType.Database
                    }
                },
                AppConfiguration = new AndroidAppConfiguration
                {
                    AppName = "TestApp",
                    PackageName = "com.example.testapp"
                }
            };
        }

        private AndroidGeneratedCode CreateInvalidAndroidGeneratedCode()
        {
            return new AndroidGeneratedCode
            {
                ComposeFiles = new List<ComposeFile>
                {
                    new ComposeFile
                    {
                        FileName = "EmptyScreen.kt",
                        Content = "", // Empty content will cause validation error
                        ViewType = ComposeViewType.Screen
                    }
                },
                RoomFiles = new List<RoomFile>
                {
                    new RoomFile
                    {
                        FileName = "EmptyDatabase.kt",
                        Content = "", // Empty content will cause validation error
                        FileType = RoomFileType.Database
                    }
                }
                // Missing AppConfiguration will cause validation error
            };
        }
    }
} 