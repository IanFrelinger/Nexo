using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Platform;
using Nexo.Infrastructure.Services.Platform;

namespace Nexo.Infrastructure.Tests.Services.Platform.Success
{
    public partial class SuccessCaseTests
    {
        private readonly Mock<ILogger<iOSCodeGenerator>> _mockIOSLogger = new();
        private readonly Mock<ILogger<AndroidCodeGenerator>> _mockAndroidLogger = new();
        private readonly Mock<ILogger<WebCodeGenerator>> _mockWebLogger = new();
        private readonly Mock<ILogger<DesktopCodeGenerator>> _mockDesktopLogger = new();
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator = new();

        private static ApplicationLogic CreateSampleApplicationLogic() => new()
        {
            ApplicationName = "SampleApp",
            Features = new List<string> { "Login", "Dashboard", "Settings" },
            DataModels = new List<string> { "User", "Session" },
            TargetPlatforms = new List<string> { "iOS", "Android", "Web", "Desktop" }
        };

        [Fact]
        public async Task iOSCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            var generator = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new iOSGenerationOptions
            {
                GenerateSwiftUI = true,
                GenerateCoreData = true,
                GenerateMetalGraphics = true,
                GenerateViewModels = true,
                GenerateServices = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated iOS code" });

            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.SwiftUI);
            Assert.NotNull(result.CoreData);
            Assert.NotNull(result.MetalGraphics);
            Assert.NotNull(result.ViewModels);
            Assert.NotNull(result.Services);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions
            {
                GenerateComposeUI = true,
                GenerateRoomDatabase = true,
                GenerateViewModels = true,
                GenerateRepositories = true,
                GenerateServices = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Android code" });

            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.ComposeUI);
            Assert.NotNull(result.RoomDatabase);
            Assert.NotNull(result.ViewModels);
            Assert.NotNull(result.Repositories);
            Assert.NotNull(result.Services);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions
            {
                Framework = "React",
                GenerateComponents = true,
                GenerateStateManagement = true,
                GenerateApiLayer = true,
                GenerateWebAssembly = true,
                GeneratePWA = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Web code" });

            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.Components);
            Assert.NotNull(result.StateManagement);
            Assert.NotNull(result.ApiLayer);
            Assert.NotNull(result.WebAssembly);
            Assert.NotNull(result.PWA);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions
            {
                Framework = "WPF",
                GenerateMVVM = true,
                GenerateServices = true,
                GenerateInstaller = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Desktop code" });

            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.FrameworkSpecificCode);
            Assert.NotNull(result.MVVM);
            Assert.NotNull(result.Services);
            Assert.NotNull(result.Installer);
            Assert.NotNull(result.Tests);
        }
    }
}


