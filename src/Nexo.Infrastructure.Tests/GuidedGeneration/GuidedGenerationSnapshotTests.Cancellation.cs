using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.GuidedGeneration;
using Xunit;

namespace Nexo.Infrastructure.Tests.GuidedGeneration
{
    /// <summary>
    /// Cancellation test cases for GuidedGenerationSnapshotTests.
    /// </summary>
    public partial class GuidedGenerationSnapshotTests
    {
        #region Cancellation Tests

        [Fact]
        public async Task ProcessStep_CancelledToken_ShouldHandleGracefully()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "test input", cts.Token);

            // Assert
            Assert.NotNull(updatedSession);
            Assert.Equal(GenerationStep.Start, updatedSession.CurrentStep);
        }

        #endregion
    }
}
