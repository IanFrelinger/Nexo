using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Performance;

namespace Nexo.Infrastructure.Tests.Services.Performance
{
    /// <summary>
    /// Cancellation test cases for ProductionPerformanceOptimizer
    /// </summary>
    public partial class ProductionPerformanceOptimizerTests
    {
        [Fact]
        public async Task OptimizePerformanceAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new PerformanceOptimizationOptions
            {
                OptimizeCaching = true,
                OptimizeMemory = true,
                OptimizeAI = true,
                OptimizeSecurity = true,
                OptimizeDatabase = true,
                OptimizeNetwork = true
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizer.OptimizePerformanceAsync(options, cts.Token));
        }

        [Fact]
        public async Task RunBenchmarkAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new PerformanceBenchmarkOptions
            {
                BenchmarkName = "Cancellation Test",
                Iterations = 10
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizer.RunBenchmarkAsync(options, cts.Token));
        }
    }
}
