using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Platform;
using Nexo.Infrastructure.Services.Platform;

namespace Nexo.Infrastructure.Tests.Services.Platform.Cancellation
{
    public class CancellationTests
    {
        private readonly Mock<ILogger<iOSCodeGenerator>> _mockIOSLogger = new();
        private readonly Mock<ILogger<AndroidCodeGenerator>> _mockAndroidLogger = new();
        private readonly Mock<ILogger<WebCodeGenerator>> _mockWebLogger = new();
        private readonly Mock<ILogger<DesktopCodeGenerator>> _mockDesktopLogger = new();
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator = new();

        private static ApplicationLogic App() => new() { ApplicationName = "SampleApp" };

        [Fact]
        public async Task iOSCodeGenerator_GenerateCodeAsync_Cancelled_Throws()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var gen = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(() => gen.GenerateCodeAsync(App(), new iOSGenerationOptions(), cts.Token));
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateCodeAsync_Cancelled_Throws()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var gen = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(() => gen.GenerateCodeAsync(App(), new AndroidGenerationOptions(), cts.Token));
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateCodeAsync_Cancelled_Throws()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var gen = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(() => gen.GenerateCodeAsync(App(), new WebGenerationOptions(), cts.Token));
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateCodeAsync_Cancelled_Throws()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var gen = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(() => gen.GenerateCodeAsync(App(), new DesktopGenerationOptions(), cts.Token));
        }
    }
}


