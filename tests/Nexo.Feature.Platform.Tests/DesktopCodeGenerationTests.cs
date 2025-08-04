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
    /// Tests for desktop code generation functionality.
    /// </summary>
    public class DesktopCodeGenerationTests
    {
        private readonly Mock<ILogger<DesktopCodeGenerator>> _mockLogger;
        private readonly DesktopCodeGenerator _desktopCodeGenerator;

        public DesktopCodeGenerationTests()
        {
            _mockLogger = new Mock<ILogger<DesktopCodeGenerator>>();
            _desktopCodeGenerator = new DesktopCodeGenerator(_mockLogger.Object);
        }

        #region Interface Tests

        [Fact]
        public void IDesktopCodeGenerator_Interface_IsDefined()
        {
            // Arrange & Act
            var generator = _desktopCodeGenerator as IDesktopCodeGenerator;

            // Assert
            Assert.NotNull(generator);
        }

        #endregion

        #region Model Tests

        [Fact]
        public void DesktopCodeGenerationRequest_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var request = new DesktopCodeGenerationRequest();

            // Assert
            Assert.NotNull(request);
            Assert.Equal(string.Empty, request.Platform);
            Assert.Equal(string.Empty, request.ApplicationType);
            Assert.Equal(string.Empty, request.UIFramework);
            Assert.Equal(string.Empty, request.ApplicationName);
            Assert.Equal(string.Empty, request.Description);
            Assert.Equal("1.0.0", request.Version);
            Assert.Equal("net8.0", request.TargetFramework);
            Assert.True(request.IncludeSystemIntegration);
            Assert.False(request.IncludeNativeAPIs);
            Assert.True(request.IncludePerformanceOptimizations);
            Assert.Equal(DesktopOptimizationLevel.Balanced, request.OptimizationLevel);
            Assert.NotNull(request.AdditionalFeatures);
            Assert.NotNull(request.CustomOptions);
        }

        [Fact]
        public void DesktopCodeGenerationRequest_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "TestApp",
                Description = "Test Application",
                Version = "2.0.0",
                TargetFramework = "net8.0",
                IncludeSystemIntegration = false,
                IncludeNativeAPIs = true,
                IncludePerformanceOptimizations = false,
                OptimizationLevel = DesktopOptimizationLevel.Maximum,
                AdditionalFeatures = new List<string> { "Feature1", "Feature2" },
                CustomOptions = new Dictionary<string, object> { { "Option1", "Value1" } }
            };

            // Assert
            Assert.Equal("Windows", request.Platform);
            Assert.Equal("WPF", request.ApplicationType);
            Assert.Equal("WPF", request.UIFramework);
            Assert.Equal("TestApp", request.ApplicationName);
            Assert.Equal("Test Application", request.Description);
            Assert.Equal("2.0.0", request.Version);
            Assert.Equal("net8.0", request.TargetFramework);
            Assert.False(request.IncludeSystemIntegration);
            Assert.True(request.IncludeNativeAPIs);
            Assert.False(request.IncludePerformanceOptimizations);
            Assert.Equal(DesktopOptimizationLevel.Maximum, request.OptimizationLevel);
            Assert.Equal(2, request.AdditionalFeatures.Count);
            Assert.Equal(1, request.CustomOptions.Count);
        }

        [Fact]
        public void DesktopCodeGenerationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new DesktopCodeGenerationResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(string.Empty, result.MainCode);
            Assert.Equal(string.Empty, result.UICode);
            Assert.NotNull(result.ConfigurationFiles);
            Assert.NotNull(result.ProjectFiles);
            Assert.Equal(string.Empty, result.SystemIntegrationCode);
            Assert.Equal(string.Empty, result.NativeAPIBindings);
            Assert.NotNull(result.DeploymentConfig);
            Assert.NotNull(result.PerformanceAnalysis);
            Assert.NotNull(result.Warnings);
            Assert.NotNull(result.Errors);
            Assert.Equal(string.Empty, result.Platform);
            Assert.Equal(string.Empty, result.ApplicationType);
            Assert.Equal(string.Empty, result.UIFramework);
        }

        [Fact]
        public void DesktopCodeGenerationResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new DesktopCodeGenerationResult
            {
                Success = true,
                MainCode = "public class Program { }",
                UICode = "public class MainWindow { }",
                ConfigurationFiles = new Dictionary<string, string> { { "appsettings.json", "{}" } },
                ProjectFiles = new Dictionary<string, string> { { "TestApp.csproj", "<Project>" } },
                SystemIntegrationCode = "// System integration",
                NativeAPIBindings = "// Native bindings",
                DeploymentConfig = new DesktopDeploymentConfig(),
                PerformanceAnalysis = new DesktopPerformanceAnalysis(),
                Warnings = new List<string> { "Warning1" },
                Errors = new List<string> { "Error1" },
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF"
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("public class Program { }", result.MainCode);
            Assert.Equal("public class MainWindow { }", result.UICode);
            Assert.Equal(1, result.ConfigurationFiles.Count);
            Assert.Equal(1, result.ProjectFiles.Count);
            Assert.Equal("// System integration", result.SystemIntegrationCode);
            Assert.Equal("// Native bindings", result.NativeAPIBindings);
            Assert.NotNull(result.DeploymentConfig);
            Assert.NotNull(result.PerformanceAnalysis);
            Assert.Equal(1, result.Warnings.Count);
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal("WPF", result.ApplicationType);
            Assert.Equal("WPF", result.UIFramework);
        }

        [Fact]
        public void DesktopCodeValidationResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new DesktopCodeValidationResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.NotNull(result.CompatibilityIssues);
            Assert.NotNull(result.PerformanceRecommendations);
            Assert.NotNull(result.SecurityRecommendations);
        }

        [Fact]
        public void DesktopCodeValidationResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new DesktopCodeValidationResult
            {
                IsValid = true,
                Errors = new List<string> { "Error1" },
                Warnings = new List<string> { "Warning1" },
                CompatibilityIssues = new List<string> { "Issue1" },
                PerformanceRecommendations = new List<string> { "Rec1" },
                SecurityRecommendations = new List<string> { "Sec1" }
            };

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Warnings.Count);
            Assert.Equal(1, result.CompatibilityIssues.Count);
            Assert.Equal(1, result.PerformanceRecommendations.Count);
            Assert.Equal(1, result.SecurityRecommendations.Count);
        }

        [Fact]
        public void DesktopDeploymentConfig_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var config = new DesktopDeploymentConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(string.Empty, config.DeploymentType);
            Assert.Equal(string.Empty, config.InstallerConfig);
            Assert.Equal(string.Empty, config.PackagingConfig);
            Assert.Equal(string.Empty, config.SigningConfig);
            Assert.Equal(string.Empty, config.UpdateConfig);
            Assert.NotNull(config.AdditionalFiles);
        }

        [Fact]
        public void DesktopDeploymentRequest_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var request = new DesktopDeploymentRequest();

            // Assert
            Assert.NotNull(request);
            Assert.Equal(string.Empty, request.DeploymentType);
            Assert.False(request.IncludeCodeSigning);
            Assert.True(request.IncludeAutoUpdates);
            Assert.True(request.IncludeCrashReporting);
            Assert.Equal(string.Empty, request.IconPath);
            Assert.NotNull(request.Metadata);
        }

        [Fact]
        public void DesktopPerformanceAnalysis_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var analysis = new DesktopPerformanceAnalysis();

            // Assert
            Assert.NotNull(analysis);
            Assert.Equal(0, analysis.EstimatedStartupTime);
            Assert.Equal(0, analysis.EstimatedMemoryUsage);
            Assert.Equal(0.0, analysis.EstimatedCpuUsage);
            Assert.Equal(0, analysis.EstimatedDiskUsage);
            Assert.NotNull(analysis.PerformanceBottlenecks);
            Assert.NotNull(analysis.OptimizationRecommendations);
            Assert.NotNull(analysis.PlatformNotes);
        }

        #endregion

        #region Enum Tests

        [Fact]
        public void DesktopOptimizationLevel_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopOptimizationLevel>();

            // Assert
            Assert.Contains(DesktopOptimizationLevel.None, values);
            Assert.Contains(DesktopOptimizationLevel.Minimal, values);
            Assert.Contains(DesktopOptimizationLevel.Balanced, values);
            Assert.Contains(DesktopOptimizationLevel.Aggressive, values);
            Assert.Contains(DesktopOptimizationLevel.Maximum, values);
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public void DesktopPlatformType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopPlatformType>();

            // Assert
            Assert.Contains(DesktopPlatformType.Windows, values);
            Assert.Contains(DesktopPlatformType.macOS, values);
            Assert.Contains(DesktopPlatformType.Linux, values);
            Assert.Contains(DesktopPlatformType.CrossPlatform, values);
            Assert.Equal(4, values.Length);
        }

        [Fact]
        public void DesktopApplicationType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopApplicationType>();

            // Assert
            Assert.Contains(DesktopApplicationType.Console, values);
            Assert.Contains(DesktopApplicationType.WinForms, values);
            Assert.Contains(DesktopApplicationType.WPF, values);
            Assert.Contains(DesktopApplicationType.WinUI, values);
            Assert.Contains(DesktopApplicationType.Avalonia, values);
            Assert.Contains(DesktopApplicationType.MAUI, values);
            Assert.Contains(DesktopApplicationType.WindowsService, values);
            Assert.Contains(DesktopApplicationType.BackgroundService, values);
            Assert.Contains(DesktopApplicationType.Library, values);
            Assert.Equal(9, values.Length);
        }

        [Fact]
        public void DesktopUIFramework_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopUIFramework>();

            // Assert
            Assert.Contains(DesktopUIFramework.WinForms, values);
            Assert.Contains(DesktopUIFramework.WPF, values);
            Assert.Contains(DesktopUIFramework.WinUI, values);
            Assert.Contains(DesktopUIFramework.Avalonia, values);
            Assert.Contains(DesktopUIFramework.MAUI, values);
            Assert.Contains(DesktopUIFramework.GTK, values);
            Assert.Contains(DesktopUIFramework.Qt, values);
            Assert.Contains(DesktopUIFramework.None, values);
            Assert.Equal(8, values.Length);
        }

        [Fact]
        public void DesktopDeploymentType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopDeploymentType>();

            // Assert
            Assert.Contains(DesktopDeploymentType.MSI, values);
            Assert.Contains(DesktopDeploymentType.MSIX, values);
            Assert.Contains(DesktopDeploymentType.EXE, values);
            Assert.Contains(DesktopDeploymentType.DMG, values);
            Assert.Contains(DesktopDeploymentType.PKG, values);
            Assert.Contains(DesktopDeploymentType.AppImage, values);
            Assert.Contains(DesktopDeploymentType.DEB, values);
            Assert.Contains(DesktopDeploymentType.RPM, values);
            Assert.Contains(DesktopDeploymentType.Portable, values);
            Assert.Equal(9, values.Length);
        }

        [Fact]
        public void DesktopSystemIntegration_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopSystemIntegration>();

            // Assert
            Assert.Contains(DesktopSystemIntegration.FileSystem, values);
            Assert.Contains(DesktopSystemIntegration.Registry, values);
            Assert.Contains(DesktopSystemIntegration.SystemTray, values);
            Assert.Contains(DesktopSystemIntegration.StartMenu, values);
            Assert.Contains(DesktopSystemIntegration.DesktopShortcut, values);
            Assert.Contains(DesktopSystemIntegration.AutoStart, values);
            Assert.Contains(DesktopSystemIntegration.Notifications, values);
            Assert.Contains(DesktopSystemIntegration.Printing, values);
            Assert.Contains(DesktopSystemIntegration.Network, values);
            Assert.Contains(DesktopSystemIntegration.Database, values);
            Assert.Equal(10, values.Length);
        }

        #endregion

        #region Service Tests

        [Fact]
        public async Task GenerateCodeAsync_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "TestApp",
                Description = "Test Application",
                Version = "1.0.0",
                TargetFramework = "net8.0"
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
            Assert.NotNull(result.ConfigurationFiles);
            Assert.NotNull(result.ProjectFiles);
            Assert.NotNull(result.DeploymentConfig);
            Assert.NotNull(result.PerformanceAnalysis);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithInvalidRequest_ReturnsFailureResult()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "InvalidPlatform",
                ApplicationType = "InvalidType",
                ApplicationName = "",
                TargetFramework = ""
            };

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Errors);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithNullRequest_ReturnsFailureResult()
        {
            // Arrange
            DesktopCodeGenerationRequest request = null;

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Errors);
        }

        [Fact]
        public void ValidateRequest_WithValidRequest_ReturnsTrue()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "TestApp",
                TargetFramework = "net8.0"
            };

            // Act
            var isValid = _desktopCodeGenerator.ValidateRequest(request);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateRequest_WithInvalidRequest_ReturnsFalse()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "InvalidPlatform",
                ApplicationType = "InvalidType",
                ApplicationName = "",
                TargetFramework = ""
            };

            // Act
            var isValid = _desktopCodeGenerator.ValidateRequest(request);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateRequest_WithNullRequest_ReturnsFalse()
        {
            // Arrange
            DesktopCodeGenerationRequest request = null;

            // Act
            var isValid = _desktopCodeGenerator.ValidateRequest(request);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetSupportedPlatforms_ReturnsExpectedPlatforms()
        {
            // Act
            var platforms = _desktopCodeGenerator.GetSupportedPlatforms().ToList();

            // Assert
            Assert.Contains("Windows", platforms);
            Assert.Contains("macOS", platforms);
            Assert.Contains("Linux", platforms);
            Assert.Contains("CrossPlatform", platforms);
            Assert.Equal(4, platforms.Count);
        }

        [Theory]
        [InlineData("Windows", new[] { "Console", "WinForms", "WPF", "WinUI", "Avalonia", "MAUI", "WindowsService", "BackgroundService", "Library" })]
        [InlineData("macOS", new[] { "Console", "Avalonia", "MAUI", "BackgroundService", "Library" })]
        [InlineData("Linux", new[] { "Console", "Avalonia", "MAUI", "BackgroundService", "Library" })]
        [InlineData("CrossPlatform", new[] { "Console", "Avalonia", "MAUI", "BackgroundService", "Library" })]
        public void GetSupportedApplicationTypes_ForPlatform_ReturnsExpectedTypes(string platform, string[] expectedTypes)
        {
            // Act
            var types = _desktopCodeGenerator.GetSupportedApplicationTypes(platform).ToList();

            // Assert
            foreach (var expectedType in expectedTypes)
            {
                Assert.Contains(expectedType, types);
            }
        }

        [Theory]
        [InlineData("Windows", new[] { "WinForms", "WPF", "WinUI", "Avalonia", "MAUI", "None" })]
        [InlineData("macOS", new[] { "Avalonia", "MAUI", "None" })]
        [InlineData("Linux", new[] { "Avalonia", "MAUI", "GTK", "Qt", "None" })]
        [InlineData("CrossPlatform", new[] { "Avalonia", "MAUI", "None" })]
        public void GetSupportedUIFrameworks_ForPlatform_ReturnsExpectedFrameworks(string platform, string[] expectedFrameworks)
        {
            // Act
            var frameworks = _desktopCodeGenerator.GetSupportedUIFrameworks(platform).ToList();

            // Assert
            foreach (var expectedFramework in expectedFrameworks)
            {
                Assert.Contains(expectedFramework, frameworks);
            }
        }

        [Fact]
        public async Task OptimizeForPlatformAsync_WithValidParameters_ReturnsOptimizedCode()
        {
            // Arrange
            var code = "var test = new Test();\nConsole.WriteLine(\"test\");";
            var platform = "Windows";
            var optimizationLevel = DesktopOptimizationLevel.Balanced;

            // Act
            var optimizedCode = await _desktopCodeGenerator.OptimizeForPlatformAsync(code, platform, optimizationLevel);

            // Assert
            Assert.NotNull(optimizedCode);
            Assert.NotEqual(code, optimizedCode);
            Assert.Contains("Balanced optimization applied", optimizedCode);
        }

        [Fact]
        public async Task GenerateSystemIntegrationAsync_WithValidParameters_ReturnsIntegrationCode()
        {
            // Arrange
            var platform = "Windows";
            var features = new[] { "FileSystem", "Registry" };

            // Act
            var integrationCode = await _desktopCodeGenerator.GenerateSystemIntegrationAsync(platform, features);

            // Assert
            Assert.NotNull(integrationCode);
            Assert.Contains("System Integration Code", integrationCode);
            Assert.Contains("Platform: Windows", integrationCode);
            Assert.Contains("FileSystem Integration", integrationCode);
            Assert.Contains("Registry Integration", integrationCode);
        }

        [Fact]
        public async Task ValidateCodeAsync_WithValidCode_ReturnsValidResult()
        {
            // Arrange
            var code = "public class Program { }";
            var platform = "Windows";

            // Act
            var result = await _desktopCodeGenerator.ValidateCodeAsync(code, platform);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCodeAsync_WithEmptyCode_ReturnsInvalidResult()
        {
            // Arrange
            var code = "";
            var platform = "Windows";

            // Act
            var result = await _desktopCodeGenerator.ValidateCodeAsync(code, platform);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Code is empty", result.Errors);
        }

        [Fact]
        public async Task GenerateDeploymentConfigAsync_WithValidParameters_ReturnsConfig()
        {
            // Arrange
            var platform = "Windows";
            var configuration = new DesktopDeploymentRequest
            {
                DeploymentType = "MSI",
                IncludeCodeSigning = true,
                IncludeAutoUpdates = true
            };

            // Act
            var config = await _desktopCodeGenerator.GenerateDeploymentConfigAsync(platform, configuration);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("MSI", config.DeploymentType);
            Assert.NotEmpty(config.InstallerConfig);
            Assert.NotEmpty(config.PackagingConfig);
            Assert.NotEmpty(config.SigningConfig);
            Assert.NotEmpty(config.UpdateConfig);
        }

        [Fact]
        public async Task AnalyzePerformanceAsync_WithValidParameters_ReturnsAnalysis()
        {
            // Arrange
            var code = "public class Program { public static void Main() { Console.WriteLine(\"Hello\"); } }";
            var platform = "Windows";

            // Act
            var analysis = await _desktopCodeGenerator.AnalyzePerformanceAsync(code, platform);

            // Assert
            Assert.NotNull(analysis);
            Assert.True(analysis.EstimatedStartupTime > 0);
            Assert.True(analysis.EstimatedMemoryUsage > 0);
            Assert.True(analysis.EstimatedCpuUsage > 0);
            Assert.True(analysis.EstimatedDiskUsage > 0);
            Assert.NotNull(analysis.PerformanceBottlenecks);
            Assert.NotNull(analysis.OptimizationRecommendations);
            Assert.NotNull(analysis.PlatformNotes);
        }

        [Fact]
        public async Task GenerateNativeAPIBindingsAsync_WithValidParameters_ReturnsBindings()
        {
            // Arrange
            var platform = "Windows";
            var apis = new[] { "Win32", "COM" };

            // Act
            var bindings = await _desktopCodeGenerator.GenerateNativeAPIBindingsAsync(platform, apis);

            // Assert
            Assert.NotNull(bindings);
            Assert.Contains("Native API Bindings", bindings);
            Assert.Contains("Platform: Windows", bindings);
            Assert.Contains("Win32 API Binding", bindings);
            Assert.Contains("COM API Binding", bindings);
        }

        #endregion

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

        #region Error Handling Tests

        [Fact]
        public async Task GenerateCodeAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "TestApp",
                TargetFramework = "net8.0"
            };

            // Mock logger to throw exception only on LogInformation calls
            var mockLogger = new Mock<ILogger<DesktopCodeGenerator>>();
            mockLogger.Setup(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                     .Throws(new InvalidOperationException("Test exception"));

            var generator = new DesktopCodeGenerator(mockLogger.Object);

            // Act
            var result = await generator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Test exception", result.Errors);
        }

        #endregion
    }
} 