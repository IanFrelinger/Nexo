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
    /// Cancellation tests for Android code generator.
    /// </summary>
    public partial class AndroidCodeGeneratorTests
    {
        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            var androidOptions = new AndroidGenerationOptions();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions, cancellationTokenSource.Token));
        }
    }
}
