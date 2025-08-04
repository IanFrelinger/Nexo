using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Project.Tests.Commands;
using System;

namespace Nexo.Feature.Project.Tests
{
    /// <summary>
    /// Pipeline-architecture test suite for Nexo.Feature.Project layer.
    /// Uses command classes with proper timeouts and logging to prevent hanging tests.
    /// </summary>
    public class ProjectPipelineTests
    {
        private readonly ILogger<ProjectPipelineTests> _logger;

        public ProjectPipelineTests()
        {
            _logger = NullLogger<ProjectPipelineTests>.Instance;
        }

        [Fact(Timeout = 10000)]
        public void Project_Interfaces_WorkCorrectly()
        {
            _logger.LogInformation("Starting Project interfaces test");
            
            var command = new ProjectValidationCommand(NullLogger<ProjectValidationCommand>.Instance);
            var result = command.ValidateProjectInterfaces(timeoutMs: 5000);
            
            Assert.True(result, "Project interfaces should work correctly");
            _logger.LogInformation("Project interfaces test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void Project_Models_WorkCorrectly()
        {
            _logger.LogInformation("Starting Project models test");
            
            var command = new ProjectValidationCommand(NullLogger<ProjectValidationCommand>.Instance);
            var result = command.ValidateProjectModels(timeoutMs: 5000);
            
            Assert.True(result, "Project models should work correctly");
            _logger.LogInformation("Project models test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void Project_UseCases_WorkCorrectly()
        {
            _logger.LogInformation("Starting Project use cases test");
            
            var command = new ProjectValidationCommand(NullLogger<ProjectValidationCommand>.Instance);
            var result = command.ValidateProjectUseCases(timeoutMs: 5000);
            
            Assert.True(result, "Project use cases should work correctly");
            _logger.LogInformation("Project use cases test completed successfully");
        }
    }
} 