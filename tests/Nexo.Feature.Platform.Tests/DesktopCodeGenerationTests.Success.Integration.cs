using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests
{
    /// <summary>
    /// Integration tests for desktop code generation functionality
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        #region Integration Tests

        [Fact]
        public async Task DesktopCodeGeneration_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "IntegrationTestApp",
                Description = "Integration Test Application",
                Version = "1.0.0",
                TargetFramework = "net8.0",
                IncludeSystemIntegration = true,
                IncludeNativeAPIs = true,
                IncludePerformanceOptimizations = true,
                OptimizationLevel = DesktopOptimizationLevel.Balanced,
                AdditionalFeatures = new List<string> { "FileSystem", "Notifications" }
            };

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal("WPF", result.ApplicationType);
            Assert.Equal("WPF", result.UIFramework);
            Assert.NotEmpty(result.MainCode);
            Assert.NotEmpty(result.UICode);
            Assert.NotEmpty(result.SystemIntegrationCode);
            Assert.NotEmpty(result.NativeAPIBindings);
            Assert.NotNull(result.ConfigurationFiles);
            Assert.NotNull(result.ProjectFiles);
            Assert.NotNull(result.DeploymentConfig);
            Assert.NotNull(result.PerformanceAnalysis);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("Windows", "WPF", "WPF")]
        [InlineData("macOS", "Avalonia", "Avalonia")]
        [InlineData("Linux", "Avalonia", "Avalonia")]
        [InlineData("CrossPlatform", "MAUI", "MAUI")]
        public async Task DesktopCodeGeneration_CrossPlatformScenarios_WorkCorrectly(string platform, string appType, string uiFramework)
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = platform,
                ApplicationType = appType,
                UIFramework = uiFramework,
                ApplicationName = $"{platform}TestApp",
                Description = $"{platform} Test Application",
                Version = "1.0.0",
                TargetFramework = "net8.0"
            };

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(platform, result.Platform);
            Assert.Equal(appType, result.ApplicationType);
            Assert.Equal(uiFramework, result.UIFramework);
            Assert.NotEmpty(result.MainCode);
            Assert.NotNull(result.ConfigurationFiles);
            Assert.NotNull(result.ProjectFiles);
        }

        #endregion
    }
}
