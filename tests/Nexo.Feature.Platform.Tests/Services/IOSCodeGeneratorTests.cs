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
    /// Unit tests for the IOSCodeGenerator service.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.1: iOS Native Implementation.
    /// </summary>
    public class IOSCodeGeneratorTests
    {
        private readonly Mock<ILogger<IOSCodeGenerator>> _mockLogger;
        private readonly IIOSCodeGenerator _generator;

        public IOSCodeGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<IOSCodeGenerator>>();
            _generator = new IOSCodeGenerator(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var iosOptions = new IOSGenerationOptions();

            // Act
            var result = await _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedCode);
            Assert.True(result.GenerationScore > 0);
            Assert.NotEmpty(result.GeneratedCode.SwiftUIFiles);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithSwiftUIDisabled_DoesNotGenerateSwiftUIFiles()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var iosOptions = new IOSGenerationOptions { EnableSwiftUI = false };

            // Act
            var result = await _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.GeneratedCode.SwiftUIFiles);
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithCoreDataEnabled_GeneratesCoreDataFiles()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var iosOptions = new IOSGenerationOptions { EnableCoreData = true };

            // Act
            var result = await _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedCode.CoreDataFiles);
            Assert.Contains(result.GeneratedCode.CoreDataFiles, f => f.FileType == CoreDataFileType.Model);
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithMetalGraphicsEnabled_GeneratesMetalFiles()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var iosOptions = new IOSGenerationOptions { EnableMetalGraphics = true };

            // Act
            var result = await _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedCode.MetalFiles);
            Assert.Contains(result.GeneratedCode.MetalFiles, f => f.FileType == MetalFileType.VertexShader);
        }

        [Fact]
        public async Task IntegrateCoreDataAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var coreDataOptions = new CoreDataOptions();

            // Act
            var result = await _generator.IntegrateCoreDataAsync(applicationLogic, coreDataOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedFiles);
            Assert.True(result.IntegrationScore > 0);
        }

        [Fact]
        public async Task CreateMetalGraphicsOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var metalOptions = new MetalGraphicsOptions();

            // Act
            var result = await _generator.CreateMetalGraphicsOptimizationAsync(applicationLogic, metalOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedFiles);
            Assert.True(result.OptimizationScore > 0);
        }

        [Fact]
        public async Task GenerateIOSUIPatternsAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var uiPatternOptions = new IOSUIPatternOptions();

            // Act
            var result = await _generator.GenerateIOSUIPatternsAsync(applicationLogic, uiPatternOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedPatterns);
            Assert.True(result.PatternScore > 0);
        }

        [Fact]
        public async Task CreateIOSPerformanceOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var performanceOptions = new IOSPerformanceOptions();

            // Act
            var result = await _generator.CreateIOSPerformanceOptimizationAsync(applicationLogic, performanceOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.GeneratedOptimizations);
            Assert.True(result.PerformanceScore > 0);
        }

        [Fact]
        public async Task GenerateIOSAppConfigurationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var configOptions = new IOSAppConfigOptions();

            // Act
            var result = await _generator.GenerateIOSAppConfigurationAsync(applicationLogic, configOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedConfiguration);
            Assert.True(result.ConfigurationScore > 0);
        }

        [Fact]
        public async Task ValidateIOSCodeAsync_WithValidCode_ReturnsValidResult()
        {
            // Arrange
            var iosCode = CreateValidIOSGeneratedCode();
            var validationOptions = new IOSValidationOptions();

            // Act
            var result = await _generator.ValidateIOSCodeAsync(iosCode, validationOptions);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ValidationScore > 0);
        }

        [Fact]
        public async Task ValidateIOSCodeAsync_WithInvalidCode_ReturnsInvalidResult()
        {
            // Arrange
            var iosCode = CreateInvalidIOSGeneratedCode();
            var validationOptions = new IOSValidationOptions();

            // Act
            var result = await _generator.ValidateIOSCodeAsync(iosCode, validationOptions);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ValidationErrors);
        }

        [Fact]
        public void GetSupportedIOSUIPatterns_ReturnsExpectedPatterns()
        {
            // Act
            var patterns = _generator.GetSupportedIOSUIPatterns().ToList();

            // Assert
            Assert.NotEmpty(patterns);
            Assert.Contains(patterns, p => p.Type == IOSUIPatternType.Navigation);
            Assert.Contains(patterns, p => p.Type == IOSUIPatternType.TabBar);
            Assert.Contains(patterns, p => p.Type == IOSUIPatternType.List);
        }

        [Fact]
        public void GetSupportedIOSPerformanceOptimizations_ReturnsExpectedOptimizations()
        {
            // Act
            var optimizations = _generator.GetSupportedIOSPerformanceOptimizations().ToList();

            // Assert
            Assert.NotEmpty(optimizations);
            Assert.Contains(optimizations, o => o.Type == IOSPerformanceType.Memory);
            Assert.Contains(optimizations, o => o.Type == IOSPerformanceType.Battery);
            Assert.Contains(optimizations, o => o.Type == IOSPerformanceType.Network);
        }

        [Fact]
        public void GetSupportedMetalGraphicsFeatures_ReturnsExpectedFeatures()
        {
            // Act
            var features = _generator.GetSupportedMetalGraphicsFeatures().ToList();

            // Assert
            Assert.NotEmpty(features);
            Assert.Contains(features, f => f.Type == MetalGraphicsFeatureType.VertexProcessing);
            Assert.Contains(features, f => f.Type == MetalGraphicsFeatureType.FragmentProcessing);
            Assert.Contains(features, f => f.Type == MetalGraphicsFeatureType.ComputeProcessing);
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithNullApplicationLogic_ThrowsArgumentNullException()
        {
            // Arrange
            StandardizedApplicationLogic applicationLogic = null;
            var iosOptions = new IOSGenerationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions));
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            IOSGenerationOptions iosOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions));
        }

        [Fact]
        public async Task GenerateSwiftUICodeAsync_WithCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var iosOptions = new IOSGenerationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _generator.GenerateSwiftUICodeAsync(applicationLogic, iosOptions, cancellationTokenSource.Token));
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
                        Name = "TestPattern",
                        Description = "Test pattern for iOS generation",
                        Type = PatternType.Repository,
                        Implementation = "Repository pattern implementation"
                    }
                },
                SecurityPatterns = new List<SecurityPattern>
                {
                    new SecurityPattern
                    {
                        Name = "TestSecurity",
                        Description = "Test security pattern",
                        Type = SecurityPatternType.Authentication,
                        Implementation = "JWT authentication"
                    }
                },
                StateManagementPatterns = new List<StateManagementPattern>
                {
                    new StateManagementPattern
                    {
                        Name = "TestState",
                        Description = "Test state management",
                        Type = StateManagementType.GlobalState,
                        Implementation = "Global state management"
                    }
                }
            };
        }

        private IOSGeneratedCode CreateValidIOSGeneratedCode()
        {
            return new IOSGeneratedCode
            {
                SwiftUIFiles = new List<SwiftUIFile>
                {
                    new SwiftUIFile
                    {
                        FileName = "ContentView.swift",
                        Content = "import SwiftUI\nstruct ContentView: View { var body: some View { Text(\"Hello\") } }",
                        ViewType = SwiftUIViewType.ContentView
                    }
                },
                AppConfiguration = new IOSAppConfiguration
                {
                    AppName = "TestApp",
                    BundleIdentifier = "com.test.app",
                    Version = "1.0.0"
                }
            };
        }

        private IOSGeneratedCode CreateInvalidIOSGeneratedCode()
        {
            return new IOSGeneratedCode
            {
                SwiftUIFiles = new List<SwiftUIFile>
                {
                    new SwiftUIFile
                    {
                        FileName = "EmptyView.swift",
                        Content = "", // Empty content should cause validation error
                        ViewType = SwiftUIViewType.ContentView
                    }
                }
                // Missing AppConfiguration should cause validation error
            };
        }
    }
} 