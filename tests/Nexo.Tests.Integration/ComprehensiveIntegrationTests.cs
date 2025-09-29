using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.Configuration;
using Nexo.Tests.Integration.TestFixtures;
using Nexo.Tests.Integration.Utilities;

namespace Nexo.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests demonstrating the full test framework capabilities
    /// </summary>
    public class ComprehensiveIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IntegrationTestConfiguration _config;

        public ComprehensiveIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = IntegrationTestConfiguration.Default;
        }

        [Fact]
        public async Task IntegrationTestRunner_FullTestSuite_ShouldExecuteSuccessfully()
        {
            // Arrange
            var runner = new IntegrationTestRunner(_config);

            // Act
            var summary = await runner.RunAllTestsAsync();

            // Assert
            Assert.True(summary.Success);
            Assert.True(summary.TotalTestSuites > 0);
            Assert.True(summary.PassedTestSuites > 0);
            Assert.Equal(0, summary.FailedTestSuites);
            Assert.True(summary.TotalTests > 0);
            Assert.True(summary.PassedTests > 0);
            Assert.Equal(0, summary.FailedTests);
            Assert.True(summary.TotalDuration.TotalMilliseconds > 0);
            Assert.NotNull(summary.TestSuiteResults);
        }

        [Fact]
        public async Task IntegrationTestRunner_WithCustomConfiguration_ShouldExecuteSuccessfully()
        {
            // Arrange
            var customConfig = new IntegrationTestConfiguration
            {
                TestEnvironment = "Custom",
                TestDataPath = "/tmp/custom-nexo-tests",
                CleanupAfterTests = true,
                GenerateCoverageReport = true,
                EnablePerformanceMonitoring = true,
                EnableResourceMonitoring = true,
                MaxConcurrentOperations = 3,
                DefaultTimeoutSeconds = 60,
                PerformanceThresholdMs = 10000,
                MemoryThresholdMB = 200
            };

            var runner = new IntegrationTestRunner(customConfig);

            // Act
            var summary = await runner.RunAllTestsAsync();

            // Assert
            Assert.True(summary.Success);
            Assert.Equal("Custom", summary.Configuration.TestEnvironment);
            Assert.Equal("/tmp/custom-nexo-tests", summary.Configuration.TestDataPath);
            Assert.True(summary.TotalTestSuites > 0);
        }

        [Fact]
        public async Task IntegrationTestRunner_WithPartialTestSuites_ShouldExecuteCorrectly()
        {
            // Arrange
            var partialConfig = new IntegrationTestConfiguration
            {
                TestSuiteSettings = new()
                {
                    { "IncludeCLITests", true },
                    { "IncludeCoreApplicationTests", true },
                    { "IncludeCoreDomainTests", false },
                    { "IncludeSharedTests", false },
                    { "IncludeIntegrationTests", true }
                }
            };

            var runner = new IntegrationTestRunner(partialConfig);

            // Act
            var summary = await runner.RunAllTestsAsync();

            // Assert
            Assert.True(summary.Success);
            Assert.Equal(3, summary.TotalTestSuites); // CLI, CoreApplication, Integration
            Assert.True(summary.PassedTestSuites > 0);
        }

        [Fact]
        public async Task SystemRequirements_ShouldBeMet()
        {
            // Act & Assert
            Assert.True(IntegrationTestHelper.CheckSystemRequirements());
        }

        [Fact]
        public async Task TestConfiguration_ShouldBeValid()
        {
            // Act & Assert
            IntegrationTestHelper.ValidateTestConfiguration(_config);
        }

        [Fact]
        public async Task TestDirectoryCreation_ShouldWork()
        {
            // Act
            var testDir = IntegrationTestHelper.CreateTestDirectory();
            
            // Assert
            Assert.True(System.IO.Directory.Exists(testDir));
            
            // Cleanup
            IntegrationTestHelper.CleanupTestDirectory(testDir);
        }

        [Fact]
        public async Task TestFileCreation_ShouldWork()
        {
            // Arrange
            var testDir = IntegrationTestHelper.CreateTestDirectory();
            
            try
            {
                // Act
                var testFile = IntegrationTestHelper.CreateTestFile(testDir, "test.txt", "Test content");
                var assemblyFile = IntegrationTestHelper.CreateTestAssembly(testDir);
                
                // Assert
                Assert.True(System.IO.File.Exists(testFile));
                Assert.True(System.IO.File.Exists(assemblyFile));
                Assert.Equal("Test content", System.IO.File.ReadAllText(testFile));
            }
            finally
            {
                // Cleanup
                IntegrationTestHelper.CleanupTestDirectory(testDir);
            }
        }

        [Fact]
        public async Task ExecutionTimeMeasurement_ShouldWork()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromMilliseconds(100);
            
            // Act
            var (result, executionTime) = await IntegrationTestHelper.MeasureExecutionTimeAsync(async () =>
            {
                await Task.Delay(expectedDelay);
                return "Test Result";
            });
            
            // Assert
            Assert.Equal("Test Result", result);
            Assert.True(executionTime >= expectedDelay);
            Assert.True(executionTime < expectedDelay.Add(TimeSpan.FromMilliseconds(50))); // Allow some tolerance
        }

        [Fact]
        public async Task MemoryUsage_ShouldBeReasonable()
        {
            // Arrange
            var initialMemory = IntegrationTestHelper.GetCurrentMemoryUsageMB();
            
            // Act
            var workflowResult = await _fixture.ExecuteCompleteWorkflowAsync();
            var finalMemory = IntegrationTestHelper.GetCurrentMemoryUsageMB();
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert
            Assert.True(workflowResult.Success);
            Assert.True(memoryIncrease < 100, $"Memory increase {memoryIncrease}MB is too high");
        }

        [Fact]
        public async Task HandleCount_ShouldNotLeak()
        {
            // Arrange
            var initialHandles = IntegrationTestHelper.GetCurrentHandleCount();
            
            // Act
            var workflowResult = await _fixture.ExecuteCompleteWorkflowAsync();
            var finalHandles = IntegrationTestHelper.GetCurrentHandleCount();
            var handleIncrease = finalHandles - initialHandles;
            
            // Assert
            Assert.True(workflowResult.Success);
            Assert.True(handleIncrease < 10, $"Handle count increased by {handleIncrease}, indicating potential leaks");
        }

        [Fact]
        public async Task ConcurrentOperations_ShouldHandleMultipleRequests()
        {
            // Arrange
            const int concurrentOperations = 3;
            var operations = new List<Func<Task<IntegrationWorkflowResult>>>();
            
            for (int i = 0; i < concurrentOperations; i++)
            {
                operations.Add(() => _fixture.ExecuteCompleteWorkflowAsync());
            }
            
            // Act
            var results = await IntegrationTestHelper.ExecuteConcurrentlyAsync(operations, 2);
            
            // Assert
            Assert.Equal(concurrentOperations, results.Count);
            foreach (var result in results)
            {
                Assert.True(result.Success);
                Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            }
        }

        [Fact]
        public async Task RetryMechanism_ShouldWork()
        {
            // Arrange
            var attemptCount = 0;
            var maxAttempts = 3;
            
            // Act
            var result = await IntegrationTestHelper.RetryAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < maxAttempts)
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                return "Success after retries";
            }, maxAttempts);
            
            // Assert
            Assert.Equal("Success after retries", result);
            Assert.Equal(maxAttempts, attemptCount);
        }

        [Fact]
        public async Task TestReportGeneration_ShouldWork()
        {
            // Arrange
            var testData = new Dictionary<string, object>
            {
                ["TestName"] = "Integration Test",
                ["TestResult"] = "Success",
                ["ExecutionTime"] = TimeSpan.FromMilliseconds(100),
                ["TestData"] = new { Value = 42, Description = "Test value" }
            };
            
            // Act
            var reportPath = IntegrationTestHelper.GenerateTestReport(testData);
            
            // Assert
            Assert.NotNull(reportPath);
            Assert.True(System.IO.File.Exists(reportPath));
            
            var reportContent = System.IO.File.ReadAllText(reportPath);
            Assert.Contains("Integration Test", reportContent);
            Assert.Contains("Success", reportContent);
            
            // Cleanup
            System.IO.File.Delete(reportPath);
        }

        [Fact]
        public async Task WaitForCondition_ShouldWork()
        {
            // Arrange
            var conditionMet = false;
            var timeout = TimeSpan.FromSeconds(2);
            
            // Start a task that will set the condition after a delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                conditionMet = true;
            });
            
            // Act
            var result = await IntegrationTestHelper.WaitForConditionAsync(
                () => conditionMet, 
                timeout, 
                TimeSpan.FromMilliseconds(100));
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WaitForCondition_WithTimeout_ShouldReturnFalse()
        {
            // Arrange
            var conditionMet = false;
            var timeout = TimeSpan.FromMilliseconds(100);
            
            // Act
            var result = await IntegrationTestHelper.WaitForConditionAsync(
                () => conditionMet, 
                timeout, 
                TimeSpan.FromMilliseconds(10));
            
            // Assert
            Assert.False(result);
        }
    }
}
