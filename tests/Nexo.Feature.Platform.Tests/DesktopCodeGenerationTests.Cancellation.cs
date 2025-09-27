using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// Cancellation tests for desktop code generation functionality.
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        #region Cancellation Tests

        [Fact]
        public async Task GenerateCodeAsync_WithCancellation_ThrowsOperationCanceledException()
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

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.GenerateCodeAsync(request, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeForPlatformAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var code = "var test = new Test();\nConsole.WriteLine(\"test\");";
            var platform = "Windows";
            var optimizationLevel = DesktopOptimizationLevel.Balanced;

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.OptimizeForPlatformAsync(code, platform, optimizationLevel, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateSystemIntegrationAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var platform = "Windows";
            var features = new[] { "FileSystem", "Registry" };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.GenerateSystemIntegrationAsync(platform, features, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ValidateCodeAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var code = "public class Program { }";
            var platform = "Windows";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.ValidateCodeAsync(code, platform, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateDeploymentConfigAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var platform = "Windows";
            var configuration = new DesktopDeploymentRequest
            {
                DeploymentType = "MSI",
                IncludeCodeSigning = true,
                IncludeAutoUpdates = true
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.GenerateDeploymentConfigAsync(platform, configuration, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AnalyzePerformanceAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var code = "public class Program { public static void Main() { Console.WriteLine(\"Hello\"); } }";
            var platform = "Windows";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.AnalyzePerformanceAsync(code, platform, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateNativeAPIBindingsAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var platform = "Windows";
            var apis = new[] { "Win32", "COM" };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.GenerateNativeAPIBindingsAsync(platform, apis, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateCodeAsync_WithCancellationDuringExecution_HandlesGracefully()
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

            var cancellationTokenSource = new CancellationTokenSource();
            
            // Cancel after a short delay to simulate cancellation during execution
            _ = Task.Run(async () =>
            {
                await Task.Delay(10);
                cancellationTokenSource.Cancel();
            });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _desktopCodeGenerator.GenerateCodeAsync(request, cancellationTokenSource.Token));
        }

        #endregion
    }
}
