using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Platform;
using Nexo.Infrastructure.Services.Platform;

namespace Nexo.Infrastructure.Tests.Services.Platform.ErrorHandling
{
    public class ErrorHandlingTests
    {
        private readonly Mock<ILogger<iOSCodeGenerator>> _mockIOSLogger = new();
        private readonly Mock<ILogger<AndroidCodeGenerator>> _mockAndroidLogger = new();
        private readonly Mock<ILogger<WebCodeGenerator>> _mockWebLogger = new();
        private readonly Mock<ILogger<DesktopCodeGenerator>> _mockDesktopLogger = new();
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator = new();

        private static ApplicationLogic CreateSampleApplicationLogic() => new() { ApplicationName = "SampleApp" };

        [Fact]
        public async Task iOSCodeGenerator_GenerateCodeAsync_ModelFailure_ReturnsFailure()
        {
            var gen = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = string.Empty, Success = false, Error = "model error" });
            var result = await gen.GenerateCodeAsync(CreateSampleApplicationLogic(), new iOSGenerationOptions());
            Assert.False(result.Success);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateCodeAsync_ModelThrows_ReturnsFailure()
        {
            var gen = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));
            var result = await gen.GenerateCodeAsync(CreateSampleApplicationLogic(), new AndroidGenerationOptions());
            Assert.False(result.Success);
            Assert.Contains("boom", result.ErrorMessage);
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateCodeAsync_InvalidOptions_ReturnsFailure()
        {
            var gen = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var options = new WebGenerationOptions { Framework = string.Empty };
            var result = await gen.GenerateCodeAsync(CreateSampleApplicationLogic(), options);
            Assert.False(result.Success);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateCodeAsync_ModelFailure_ReturnsFailure()
        {
            var gen = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Success = false, Error = "bad" });
            var result = await gen.GenerateCodeAsync(CreateSampleApplicationLogic(), new DesktopGenerationOptions());
            Assert.False(result.Success);
            Assert.Contains("bad", result.ErrorMessage);
        }
    }
}


