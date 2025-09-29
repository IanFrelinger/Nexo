using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.TestFixtures;

namespace Nexo.Tests.Integration.Scenarios
{
    /// <summary>
    /// Performance and load testing for integration scenarios
    /// </summary>
    public class PerformanceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public PerformanceIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ProjectCreation_Performance_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            const int maxExecutionTimeMs = 5000; // 5 seconds

            // Act
            var result = await _fixture.CreateTestProjectAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(stopwatch.ElapsedMilliseconds < maxExecutionTimeMs, 
                $"Project creation took {stopwatch.ElapsedMilliseconds}ms, expected < {maxExecutionTimeMs}ms");
        }

        [Fact]
        public async Task AgentCreation_Performance_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            const int maxExecutionTimeMs = 3000; // 3 seconds

            // Act
            var result = await _fixture.CreateTestAgentAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(stopwatch.ElapsedMilliseconds < maxExecutionTimeMs, 
                $"Agent creation took {stopwatch.ElapsedMilliseconds}ms, expected < {maxExecutionTimeMs}ms");
        }

        [Fact]
        public async Task AssemblyAnalysis_Performance_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            const int maxExecutionTimeMs = 2000; // 2 seconds

            // Act
            var result = await _fixture.AnalyzeTestAssemblyAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(stopwatch.ElapsedMilliseconds < maxExecutionTimeMs, 
                $"Assembly analysis took {stopwatch.ElapsedMilliseconds}ms, expected < {maxExecutionTimeMs}ms");
        }

        [Fact]
        public async Task CompleteWorkflow_Performance_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            const int maxExecutionTimeMs = 10000; // 10 seconds

            // Act
            var result = await _fixture.ExecuteCompleteWorkflowAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(result.Success);
            Assert.True(stopwatch.ElapsedMilliseconds < maxExecutionTimeMs, 
                $"Complete workflow took {stopwatch.ElapsedMilliseconds}ms, expected < {maxExecutionTimeMs}ms");
        }

        [Fact]
        public async Task ConcurrentOperations_ShouldHandleMultipleRequests()
        {
            // Arrange
            const int concurrentOperations = 5;
            var tasks = new List<Task<IntegrationWorkflowResult>>();

            // Act
            for (int i = 0; i < concurrentOperations; i++)
            {
                tasks.Add(_fixture.ExecuteCompleteWorkflowAsync());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(concurrentOperations, results.Length);
            foreach (var result in results)
            {
                Assert.True(result.Success);
                Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public async Task LoadTest_WithMultipleIterations_ShouldMaintainPerformance(int iterations)
        {
            // Arrange
            var executionTimes = new List<TimeSpan>();
            const int maxAverageExecutionTimeMs = 8000; // 8 seconds average

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = await _fixture.ExecuteCompleteWorkflowAsync();
                executionTimes.Add(result.ExecutionTime);
                
                Assert.True(result.Success, $"Iteration {i + 1} failed");
            }

            // Assert
            var averageExecutionTime = TimeSpan.FromMilliseconds(
                executionTimes.Average(t => t.TotalMilliseconds));
            
            Assert.True(averageExecutionTime.TotalMilliseconds < maxAverageExecutionTimeMs, 
                $"Average execution time {averageExecutionTime.TotalMilliseconds}ms exceeded limit {maxAverageExecutionTimeMs}ms");
        }

        [Fact]
        public async Task MemoryUsage_ShouldNotExceedReasonableLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            const long maxMemoryIncreaseBytes = 50 * 1024 * 1024; // 50MB

            // Act
            var result = await _fixture.ExecuteCompleteWorkflowAsync();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.True(result.Success);
            Assert.True(memoryIncrease < maxMemoryIncreaseBytes, 
                $"Memory increase {memoryIncrease / 1024 / 1024}MB exceeded limit {maxMemoryIncreaseBytes / 1024 / 1024}MB");
        }

        [Fact]
        public async Task ResourceCleanup_ShouldNotLeaveOrphanedResources()
        {
            // Arrange
            var initialHandleCount = Process.GetCurrentProcess().HandleCount;

            // Act
            var result = await _fixture.ExecuteCompleteWorkflowAsync();
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalHandleCount = Process.GetCurrentProcess().HandleCount;
            var handleIncrease = finalHandleCount - initialHandleCount;

            // Assert
            Assert.True(result.Success);
            Assert.True(handleIncrease < 10, 
                $"Handle count increased by {handleIncrease}, indicating potential resource leaks");
        }
    }
}
