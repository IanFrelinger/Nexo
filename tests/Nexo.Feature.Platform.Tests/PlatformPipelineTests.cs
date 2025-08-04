using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Platform.Tests.Commands;
using System;

namespace Nexo.Feature.Platform.Tests
{
    /// <summary>
    /// Pipeline-architecture test suite for Nexo.Feature.Platform layer.
    /// Uses command classes with proper timeouts and logging to prevent hanging tests.
    /// </summary>
    public class PlatformPipelineTests
    {
        private readonly ILogger<PlatformPipelineTests> _logger;

        public PlatformPipelineTests()
        {
            _logger = NullLogger<PlatformPipelineTests>.Instance;
        }

        [Fact(Timeout = 10000)]
        public void Platform_Interfaces_WorkCorrectly()
        {
            _logger.LogInformation("Starting Platform interfaces test");
            
            var command = new PlatformValidationCommand(NullLogger<PlatformValidationCommand>.Instance);
            var result = command.ValidatePlatformInterfaces(timeoutMs: 5000);
            
            Assert.True(result, "Platform interfaces should work correctly");
            _logger.LogInformation("Platform interfaces test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void Platform_Models_WorkCorrectly()
        {
            _logger.LogInformation("Starting Platform models test");
            
            var command = new PlatformValidationCommand(NullLogger<PlatformValidationCommand>.Instance);
            var result = command.ValidatePlatformModels(timeoutMs: 5000);
            
            Assert.True(result, "Platform models should work correctly");
            _logger.LogInformation("Platform models test completed successfully");
        }
    }
} 