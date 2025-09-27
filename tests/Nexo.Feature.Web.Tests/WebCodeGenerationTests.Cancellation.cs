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
    /// Initialization and cancellation tests for web code generation functionality
    /// </summary>
    public partial class WebCodeGenerationTests
    {
        [Fact]
        public void WebCodeGenerationRequest_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var request = new WebCodeGenerationRequest();

            // Assert
            Assert.NotNull(request);
            Assert.Equal(WebFrameworkType.React, request.Framework);
            Assert.Equal(WebComponentType.Functional, request.ComponentType);
            Assert.Equal(string.Empty, request.ComponentName);
            Assert.Equal(string.Empty, request.SourceCode);
            Assert.Equal(string.Empty, request.TargetPath);
            Assert.Equal(WebAssemblyOptimization.Balanced, request.Optimization);
            Assert.True(request.IncludeTypeScript);
            Assert.True(request.IncludeStyling);
            Assert.False(request.IncludeTests);
            Assert.True(request.IncludeDocumentation);
            Assert.NotNull(request.Options);
            Assert.NotNull(request.WebAssemblySettings);
        }

        [Fact]
        public void WebCodeGenerationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new WebCodeGenerationResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(string.Empty, result.Message);
            Assert.Equal(string.Empty, result.ComponentCode);
            Assert.Equal(string.Empty, result.TypeScriptTypes);
            Assert.Equal(string.Empty, result.StylingCode);
            Assert.Equal(string.Empty, result.TestCode);
            Assert.Equal(string.Empty, result.Documentation);
            Assert.NotNull(result.WebAssemblyMetrics);
            Assert.NotNull(result.PerformanceMetrics);
            Assert.NotNull(result.BundleSizes);
            Assert.NotNull(result.GeneratedFiles);
            Assert.NotNull(result.Warnings);
        }

        [Fact]
        public void WebAssemblyConfig_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var config = new WebAssemblyConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(WebAssemblyOptimization.Balanced, config.Optimization);
            Assert.True(config.EnableTreeShaking);
            Assert.True(config.EnableCodeSplitting);
            Assert.True(config.EnableMinification);
            Assert.False(config.EnableSourceMaps);
            Assert.NotNull(config.TargetBrowsers);
            Assert.NotNull(config.CustomFlags);
            Assert.NotNull(config.Memory);
            Assert.NotNull(config.Threading);
            Assert.NotNull(config.Simd);
        }
    }
}
