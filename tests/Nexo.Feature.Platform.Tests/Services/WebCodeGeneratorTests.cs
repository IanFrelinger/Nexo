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
    /// Unit tests for the WebCodeGenerator service.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.3: Web Implementation.
    /// </summary>
    public class WebCodeGeneratorTests
    {
        private readonly Mock<ILogger<WebCodeGenerator>> _mockLogger;
        private readonly IWebCodeGenerator _generator;

        public WebCodeGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<WebCodeGenerator>>();
            _generator = new WebCodeGenerator(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateReactCodeAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var webOptions = new WebGenerationOptions
            {
                EnableReact = true,
                EnableTypeScript = true,
                EnableWebAssembly = true,
                EnableProgressiveWebApp = true,
                EnablePerformanceOptimization = true
            };

            // Act
            var result = await _generator.GenerateReactCodeAsync(applicationLogic, webOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedCode);
            Assert.True(result.GenerationScore > 0);
            Assert.NotEmpty(result.GeneratedCode.JavaScriptFiles);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        [Fact]
        public async Task GenerateReactCodeAsync_WithReactDisabled_DoesNotGenerateJavaScriptFiles()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var webOptions = new WebGenerationOptions
            {
                EnableReact = false,
                EnableTypeScript = true
            };

            // Act
            var result = await _generator.GenerateReactCodeAsync(applicationLogic, webOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.GeneratedCode.JavaScriptFiles);
        }

        [Fact]
        public async Task GenerateVueCodeAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var webOptions = new WebGenerationOptions
            {
                EnableVue = true,
                EnableTypeScript = true,
                EnableWebAssembly = true,
                EnableProgressiveWebApp = true,
                EnablePerformanceOptimization = true
            };

            // Act
            var result = await _generator.GenerateVueCodeAsync(applicationLogic, webOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedCode);
            Assert.True(result.GenerationScore > 0);
            Assert.NotEmpty(result.GeneratedCode.JavaScriptFiles);
            Assert.NotNull(result.GeneratedCode.AppConfiguration);
        }

        [Fact]
        public async Task CreateWebAssemblyOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var wasmOptions = new WebAssemblyOptions();

            // Act
            var result = await _generator.CreateWebAssemblyOptimizationAsync(applicationLogic, wasmOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedFiles);
            Assert.True(result.OptimizationScore > 0);
            Assert.NotEmpty(result.GeneratedFiles);
        }

        [Fact]
        public async Task GenerateProgressiveWebAppFeaturesAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var pwaOptions = new ProgressiveWebAppOptions();

            // Act
            var result = await _generator.GenerateProgressiveWebAppFeaturesAsync(applicationLogic, pwaOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedFiles);
            Assert.True(result.PWAScore > 0);
            Assert.NotEmpty(result.GeneratedFiles);
        }

        [Fact]
        public async Task GenerateWebUIPatternsAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var uiPatternOptions = new WebUIPatternOptions();

            // Act
            var result = await _generator.GenerateWebUIPatternsAsync(applicationLogic, uiPatternOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedPatterns);
            Assert.True(result.PatternScore > 0);
            Assert.NotEmpty(result.GeneratedPatterns);
        }

        [Fact]
        public async Task CreateWebPerformanceOptimizationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var performanceOptions = new WebPerformanceOptions();

            // Act
            var result = await _generator.CreateWebPerformanceOptimizationAsync(applicationLogic, performanceOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedOptimizations);
            Assert.True(result.PerformanceScore > 0);
            Assert.NotEmpty(result.GeneratedOptimizations);
        }

        [Fact]
        public async Task GenerateWebAppConfigurationAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var configOptions = new WebAppConfigOptions();

            // Act
            var result = await _generator.GenerateWebAppConfigurationAsync(applicationLogic, configOptions);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedConfiguration);
            Assert.True(result.ConfigurationScore > 0);
            Assert.NotNull(result.GeneratedConfiguration);
        }

        [Fact]
        public async Task ValidateWebCodeAsync_WithValidWebCode_ReturnsSuccessResult()
        {
            // Arrange
            var webCode = CreateValidWebGeneratedCode();
            var validationOptions = new WebValidationOptions();

            // Act
            var result = await _generator.ValidateWebCodeAsync(webCode, validationOptions);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ValidationScore > 0.8);
            Assert.Empty(result.ValidationErrors.Where(e => e.Contains("Critical")));
        }

        [Fact]
        public async Task GenerateReactCodeAsync_WithNullApplicationLogic_ThrowsArgumentNullException()
        {
            // Arrange
            StandardizedApplicationLogic applicationLogic = null;
            var webOptions = new WebGenerationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _generator.GenerateReactCodeAsync(applicationLogic, webOptions));
        }

        [Fact]
        public async Task GenerateReactCodeAsync_WithNullWebOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            WebGenerationOptions webOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _generator.GenerateReactCodeAsync(applicationLogic, webOptions));
        }

        [Fact]
        public void GetSupportedWebUIPatterns_ReturnsNonEmptyCollection()
        {
            // Act
            var patterns = _generator.GetSupportedWebUIPatterns();

            // Assert
            Assert.NotNull(patterns);
            Assert.NotEmpty(patterns);
        }

        [Fact]
        public void GetSupportedWebPerformanceOptimizations_ReturnsNonEmptyCollection()
        {
            // Act
            var optimizations = _generator.GetSupportedWebPerformanceOptimizations();

            // Assert
            Assert.NotNull(optimizations);
            Assert.NotEmpty(optimizations);
        }

        [Fact]
        public void GetSupportedWebAssemblyFeatures_ReturnsNonEmptyCollection()
        {
            // Act
            var features = _generator.GetSupportedWebAssemblyFeatures();

            // Assert
            Assert.NotNull(features);
            Assert.NotEmpty(features);
        }

        private StandardizedApplicationLogic CreateValidApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>
                {
                    new ApplicationPattern
                    {
                        Name = "Repository Pattern",
                        Type = PatternType.Repository,
                        Implementation = "Standard repository implementation"
                    }
                },
                SecurityPatterns = new List<SecurityPattern>
                {
                    new SecurityPattern
                    {
                        Name = "JWT Authentication",
                        Type = SecurityPatternType.JWT,
                        Implementation = "JWT token authentication"
                    }
                },
                StateManagementPatterns = new List<StateManagementPattern>
                {
                    new StateManagementPattern
                    {
                        Name = "Redux Pattern",
                        Type = StateManagementType.Redux,
                        Implementation = "Redux state management"
                    }
                },
                ApiContracts = new List<ApiContract>
                {
                    new ApiContract
                    {
                        Name = "User API",
                        Endpoint = "/api/users",
                        Method = Nexo.Feature.AI.Models.HttpMethod.GET,
                        Parameters = new List<ApiParameter>(),
                        Response = new ApiResponse()
                    }
                }
            };
        }

        private WebGeneratedCode CreateValidWebGeneratedCode()
        {
            return new WebGeneratedCode
            {
                JavaScriptFiles = new List<JavaScriptFile>
                {
                    new JavaScriptFile
                    {
                        FileName = "app.js",
                        FilePath = "src/app.js",
                        Content = "console.log('Hello World');",
                        FileType = JavaScriptFileType.Component
                    }
                },
                TypeScriptFiles = new List<TypeScriptFile>
                {
                    new TypeScriptFile
                    {
                        FileName = "types.ts",
                        FilePath = "src/types.ts",
                        Content = "interface User { id: number; name: string; }",
                        FileType = TypeScriptFileType.Type
                    }
                },
                WebAssemblyFiles = new List<WebAssemblyFile>
                {
                    new WebAssemblyFile
                    {
                        FileName = "module.wasm",
                        FilePath = "src/wasm/module.wasm",
                        Content = "WebAssembly binary content",
                        FileType = WebAssemblyFileType.Module
                    }
                },
                AppliedUIPatterns = new List<WebUIPattern>
                {
                    new WebUIPattern
                    {
                        Name = "Responsive Design",
                        Type = WebUIPatternType.Component,
                        Implementation = "CSS Grid and Flexbox"
                    }
                },
                AppliedOptimizations = new List<WebPerformanceOptimization>
                {
                    new WebPerformanceOptimization
                    {
                        Name = "Code Splitting",
                        Type = WebPerformanceType.CodeSplitting,
                        Implementation = "Dynamic imports"
                    }
                },
                AppConfiguration = new WebAppConfiguration
                {
                    AppName = "Test App",
                    Description = "Test application"
                }
            };
        }
    }
} 