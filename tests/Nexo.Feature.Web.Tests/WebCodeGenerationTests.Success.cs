using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.Services;
using Nexo.Feature.Web.UseCases;
using Moq;
using Nexo.Feature.Web.Interfaces;

namespace Nexo.Feature.Web.Tests
{
    /// <summary>
    /// Success tests for web code generation functionality
    /// </summary>
    public partial class WebCodeGenerationTests
    {
        [Fact]
        public void WebFrameworkType_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.React));
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.Vue));
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.Angular));
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.Svelte));
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.NextJs));
            Assert.True(Enum.IsDefined(typeof(WebFrameworkType), WebFrameworkType.NuxtJs));
        }

        [Fact]
        public void WebComponentType_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Functional));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Class));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Pure));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.HigherOrder));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Hook));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Context));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Page));
            Assert.True(Enum.IsDefined(typeof(WebComponentType), WebComponentType.Layout));
        }

        [Fact]
        public void WebAssemblyOptimization_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.None));
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.Basic));
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.Aggressive));
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.Size));
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.Balanced));
            Assert.True(Enum.IsDefined(typeof(WebAssemblyOptimization), WebAssemblyOptimization.Custom));
        }

        [Fact]
        public void WebCodeGenerationRequest_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var request = new WebCodeGenerationRequest
            {
                Framework = WebFrameworkType.React,
                ComponentType = WebComponentType.Functional,
                ComponentName = "TestComponent",
                SourceCode = "console.log('test');",
                TargetPath = "/src/components",
                Optimization = WebAssemblyOptimization.Balanced,
                IncludeTypeScript = true,
                IncludeStyling = true,
                IncludeTests = false,
                IncludeDocumentation = true
            };

            // Assert
            Assert.Equal(WebFrameworkType.React, request.Framework);
            Assert.Equal(WebComponentType.Functional, request.ComponentType);
            Assert.Equal("TestComponent", request.ComponentName);
            Assert.Equal("console.log('test');", request.SourceCode);
            Assert.Equal("/src/components", request.TargetPath);
            Assert.Equal(WebAssemblyOptimization.Balanced, request.Optimization);
            Assert.True(request.IncludeTypeScript);
            Assert.True(request.IncludeStyling);
            Assert.False(request.IncludeTests);
            Assert.True(request.IncludeDocumentation);
        }

        [Fact]
        public void WebCodeGenerationResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new WebCodeGenerationResult
            {
                Success = true,
                Message = "Generation successful",
                ComponentCode = "export default function TestComponent() { return <div>Test</div>; }",
                TypeScriptTypes = "interface TestComponentProps {}",
                StylingCode = ".test-component { color: red; }",
                TestCode = "describe('TestComponent', () => { it('renders', () => {}); });",
                Documentation = "# TestComponent\n\nA test component.",
                Framework = WebFrameworkType.React,
                ComponentType = WebComponentType.Functional,
                Optimization = WebAssemblyOptimization.Balanced
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Generation successful", result.Message);
            Assert.Contains("export default function TestComponent", result.ComponentCode);
            Assert.Contains("interface TestComponentProps", result.TypeScriptTypes);
            Assert.Contains(".test-component", result.StylingCode);
            Assert.Contains("describe('TestComponent'", result.TestCode);
            Assert.Contains("# TestComponent", result.Documentation);
            Assert.Equal(WebFrameworkType.React, result.Framework);
            Assert.Equal(WebComponentType.Functional, result.ComponentType);
            Assert.Equal(WebAssemblyOptimization.Balanced, result.Optimization);
        }

        [Fact]
        public void WebAssemblyConfig_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var config = new WebAssemblyConfig
            {
                Optimization = WebAssemblyOptimization.Aggressive,
                EnableTreeShaking = true,
                EnableCodeSplitting = true,
                EnableMinification = true,
                EnableSourceMaps = false,
                TargetBrowsers = new List<string> { "chrome >= 80", "firefox >= 78" }
            };

            // Assert
            Assert.Equal(WebAssemblyOptimization.Aggressive, config.Optimization);
            Assert.True(config.EnableTreeShaking);
            Assert.True(config.EnableCodeSplitting);
            Assert.True(config.EnableMinification);
            Assert.False(config.EnableSourceMaps);
            Assert.Equal(2, config.TargetBrowsers.Count);
            Assert.Contains("chrome >= 80", config.TargetBrowsers);
            Assert.Contains("firefox >= 78", config.TargetBrowsers);
        }

        [Fact]
        public async Task WebCodeGenerator_WithValidRequest_GeneratesCodeSuccessfully()
        {
            // Arrange
            var mockTemplateProvider = new Mock<IFrameworkTemplateProvider>();
            var mockWasmOptimizer = new Mock<IWebAssemblyOptimizer>();
            var logger = NullLogger<WebCodeGenerator>.Instance;

            mockTemplateProvider.Setup(x => x.GetTemplate(It.IsAny<WebFrameworkType>(), It.IsAny<WebComponentType>()))
                .Returns("export default function {{ComponentName}}() { return <div>{{ComponentName}}</div>; }");

            mockTemplateProvider.Setup(x => x.GetTypeScriptTemplate(It.IsAny<WebFrameworkType>(), It.IsAny<WebComponentType>()))
                .Returns("interface {{ComponentName}}Props {}");

            mockTemplateProvider.Setup(x => x.GetStylingTemplate(It.IsAny<WebFrameworkType>(), It.IsAny<WebComponentType>()))
                .Returns(".{{ComponentName}}-container { }");

            mockTemplateProvider.Setup(x => x.GetTestTemplate(It.IsAny<WebFrameworkType>(), It.IsAny<WebComponentType>()))
                .Returns("describe('{{ComponentName}}', () => {});");

            mockTemplateProvider.Setup(x => x.GetDocumentationTemplate(It.IsAny<WebFrameworkType>(), It.IsAny<WebComponentType>()))
                .Returns("# {{ComponentName}}\n\nDocumentation");

            mockWasmOptimizer.Setup(x => x.OptimizeAsync(It.IsAny<string>(), It.IsAny<WebAssemblyConfig>()))
                .ReturnsAsync(new WebAssemblyOptimizationResult { Success = true, OptimizedCode = "optimized code" });

            mockWasmOptimizer.Setup(x => x.AnalyzePerformanceAsync(It.IsAny<string>()))
                .ReturnsAsync(new WebAssemblyPerformanceAnalysis());

            mockWasmOptimizer.Setup(x => x.EstimateBundleSizeAsync(It.IsAny<string>(), It.IsAny<WebAssemblyConfig>()))
                .ReturnsAsync(new WebAssemblyBundleAnalysis());

            var generator = new WebCodeGenerator(logger, mockTemplateProvider.Object, mockWasmOptimizer.Object);

            var request = new WebCodeGenerationRequest
            {
                Framework = WebFrameworkType.React,
                ComponentType = WebComponentType.Functional,
                ComponentName = "TestComponent",
                TargetPath = "/src/components",
                Optimization = WebAssemblyOptimization.Balanced,
                IncludeTypeScript = true,
                IncludeStyling = true,
                IncludeTests = true,
                IncludeDocumentation = true
            };

            // Act
            var result = await generator.GenerateCodeAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(WebFrameworkType.React, result.Framework);
            Assert.Equal(WebComponentType.Functional, result.ComponentType);
            Assert.Equal(WebAssemblyOptimization.Balanced, result.Optimization);
        }

        [Fact]
        public async Task WebAssemblyOptimizer_WithValidConfig_OptimizesCodeSuccessfully()
        {
            // Arrange
            var logger = NullLogger<WebAssemblyOptimizer>.Instance;
            var optimizer = new WebAssemblyOptimizer(logger);

            var config = new WebAssemblyConfig
            {
                Optimization = WebAssemblyOptimization.Balanced,
                EnableTreeShaking = true,
                EnableMinification = true
            };

            var sourceCode = @"
                import React from 'react';
                export default function TestComponent() {
                    return <div>Test</div>;
                }
            ";

            // Act
            var result = await optimizer.OptimizeAsync(sourceCode, config);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedCode);
            Assert.True(result.OptimizationTime > TimeSpan.Zero);
            Assert.NotNull(result.Metrics);
        }

        [Fact]
        public async Task WebAssemblyOptimizer_AnalyzesPerformance_Correctly()
        {
            // Arrange
            var logger = NullLogger<WebAssemblyOptimizer>.Instance;
            var optimizer = new WebAssemblyOptimizer(logger);

            var sourceCode = @"
                function complexFunction() {
                    if (condition1 && condition2) {
                        for (let i = 0; i < 100; i++) {
                            if (i % 2 === 0) {
                                console.log(i);
                            }
                        }
                    }
                }
            ";

            // Act
            var result = await optimizer.AnalyzePerformanceAsync(sourceCode);

            // Assert
            Assert.NotNull(result.PerformanceMetrics);
            Assert.True(result.PerformanceMetrics.ContainsKey("complexity"));
            Assert.True(result.PerformanceMetrics.ContainsKey("memoryEfficiency"));
            Assert.True(result.PerformanceMetrics.ContainsKey("executionEfficiency"));
            Assert.True(result.PerformanceMetrics.ContainsKey("bundleEfficiency"));
        }

        [Fact]
        public void FrameworkTemplateProvider_WithValidFramework_ReturnsTemplate()
        {
            // Arrange
            var logger = NullLogger<FrameworkTemplateProvider>.Instance;
            var provider = new FrameworkTemplateProvider(logger);

            // Act
            var template = provider.GetTemplate(WebFrameworkType.React, WebComponentType.Functional);

            // Assert
            Assert.NotNull(template);
            Assert.Contains("{{ComponentName}}", template);
            Assert.Contains("{{Framework}}", template);
        }

        [Fact]
        public async Task GenerateWebCodeUseCase_WithValidRequest_ExecutesSuccessfully()
        {
            // Arrange
            var mockCodeGenerator = new Mock<IWebCodeGenerator>();
            var mockWasmOptimizer = new Mock<IWebAssemblyOptimizer>();
            var logger = NullLogger<GenerateWebCodeUseCase>.Instance;

            mockCodeGenerator.Setup(x => x.ValidateRequest(It.IsAny<WebCodeGenerationRequest>()))
                .Returns(true);

            mockCodeGenerator.Setup(x => x.GenerateCodeAsync(It.IsAny<WebCodeGenerationRequest>()))
                .ReturnsAsync(new WebCodeGenerationResult { Success = true });

            var useCase = new GenerateWebCodeUseCase(logger, mockCodeGenerator.Object, mockWasmOptimizer.Object);

            var request = new WebCodeGenerationRequest
            {
                Framework = WebFrameworkType.React,
                ComponentType = WebComponentType.Functional,
                ComponentName = "TestComponent",
                TargetPath = "/src/components"
            };

            // Act
            var result = await useCase.ExecuteAsync(request);

            // Assert
            Assert.True(result.Success);
            mockCodeGenerator.Verify(x => x.ValidateRequest(It.IsAny<WebCodeGenerationRequest>()), Times.Once);
            mockCodeGenerator.Verify(x => x.GenerateCodeAsync(It.IsAny<WebCodeGenerationRequest>()), Times.Once);
        }
    }
}
