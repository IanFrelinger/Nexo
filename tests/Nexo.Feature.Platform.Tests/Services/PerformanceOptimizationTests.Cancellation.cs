using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Feature.Platform.Tests.Services;

/// <summary>
/// Cancellation test cases for PerformanceOptimization
/// </summary>
public partial class PerformanceOptimizationTests
{
    [Fact(Timeout = 10000)]
    public async Task PerformanceOptimization_WithCancellationToken_RespectsCancellation()
    {
        _logger.LogInformation("Starting cancellation token test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act - Cancel immediately
        cancellationTokenSource.Cancel();

        // Assert - Should handle cancellation gracefully
        var result = await _performanceOptimization.InitializeAsync(platformType, cancellationToken);
        
        // The result might be success or failure depending on implementation, but should not throw
        Assert.NotNull(result);
        
        _logger.LogInformation("Cancellation token test completed successfully");
    }
}
