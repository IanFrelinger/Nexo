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
/// Error handling test cases for PerformanceOptimization
/// </summary>
public partial class PerformanceOptimizationTests
{
    [Fact(Timeout = 10000)]
    public async Task PerformanceOptimization_ErrorHandling_WorksCorrectly()
    {
        _logger.LogInformation("Starting error handling test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act - Test with invalid tuning profile (not supported on current platform)
        var invalidProfile = new PerformanceTuningProfile
        {
            Name = "Invalid Profile",
            Description = "Profile not supported on current platform",
            Type = TuningProfileType.Balanced,
            SupportedPlatforms = new List<PlatformType> { PlatformType.MacOS } // Not supported on Windows
        };
        
        var result = await _performanceOptimization.ApplyPerformanceTuningAsync(invalidProfile);

        // Assert - Should handle unsupported platform gracefully
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("not supported", result.Message);
        
        _logger.LogInformation("Error handling test completed successfully");
    }
}
