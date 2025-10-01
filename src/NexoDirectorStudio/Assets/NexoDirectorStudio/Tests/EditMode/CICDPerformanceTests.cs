using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// CI/CD tests for performance and stress testing.
    /// </summary>
    public class CICDPerformanceTests
    {
        private DirectorStudioService _service;
        private TestRunnerIntegration _testRunner;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
            _testRunner = new TestRunnerIntegration(maxRetries: 1, timeoutSeconds: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        [Timeout(60000)] // 60 second timeout
        public async Task CICD_StressTest_ShouldMaintainStability()
        {
            // Arrange
            var briefs = Enumerable.Range(1, 10).Select(i => 
                CICDTestUtilities.CreateTestBrief($"CI/CD stress test {i}", 
                    i % 3 == 0 ? "FPS" : i % 3 == 1 ? "Platformer" : "RPG", 4000 + i)).ToArray();

            var successCount = 0;
            var errorCount = 0;

            // Act - Run stress test
            var tasks = briefs.Select(async brief =>
            {
                try
                {
                    var result = await CICDTestUtilities.RunMinimalPipelineAsync(brief);
                    if (result.Success)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                    return result.Success;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"CI/CD stress test pipeline failed: {ex.Message}");
                    Interlocked.Increment(ref errorCount);
                    return false;
                }
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            var successRate = (double)successCount / briefs.Length * 100;
            Assert.IsTrue(successRate >= 80, 
                $"Success rate should be >= 80%, was {successRate:F1}% ({successCount}/{briefs.Length})");

            Debug.Log($"✅ CI/CD stress test passed - {successRate:F1}% success rate");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_MemoryUsage_ShouldBeReasonable()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Run a few pipelines
            for (int i = 0; i < 5; i++)
            {
                var brief = CICDTestUtilities.CreateTestBrief($"CI/CD memory test {i}", "FPS", 5000 + i);
                await CICDTestUtilities.RunMinimalPipelineAsync(brief);

                // Force cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, // 50MB limit
                $"Memory increase should be < 50MB, was {memoryIncrease / 1024 / 1024}MB");

            Debug.Log($"✅ CI/CD memory usage test passed - {memoryIncrease / 1024 / 1024}MB increase");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_DeterministicGeneration_ShouldBeReproducible()
        {
            // Arrange
            var brief = CICDTestUtilities.CreateTestBrief("CI/CD deterministic test", "FPS", 6000);

            // Act - Run pipeline twice with same seed
            var result1 = await CICDTestUtilities.RunMinimalPipelineAsync(brief);
            var result2 = await CICDTestUtilities.RunMinimalPipelineAsync(brief);

            // Assert
            Assert.IsTrue(result1.Success, "First run should succeed");
            Assert.IsTrue(result2.Success, "Second run should succeed");

            // Compare key properties for determinism
            Assert.AreEqual(result1.GamePlan.Genre, result2.GamePlan.Genre, "Genre should be identical");
            Assert.AreEqual(result1.GamePlan.CoreMechanics.Count, result2.GamePlan.CoreMechanics.Count, 
                "Core mechanics count should be identical");
            Assert.AreEqual(result1.WorldLayout.Dimensions, result2.WorldLayout.Dimensions, 
                "World dimensions should be identical");
            Assert.AreEqual(result1.InteractionGraph.Nodes.Count, result2.InteractionGraph.Nodes.Count, 
                "Interaction nodes count should be identical");

            Debug.Log("✅ CI/CD deterministic generation test passed");
        }
    }
}
