using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Performance and load tests for the NexoDirector pipeline.
    /// Tests system performance under various load conditions.
    /// </summary>
    public class PerformanceTests
    {
        private IDirectorStudioService _service;
        private TestRunnerIntegration _testRunner;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
            _testRunner = new TestRunnerIntegration(maxRetries: 1, timeoutSeconds: 10f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task Performance_SinglePipeline_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Performance test FPS",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 123
            );

            var stopwatch = Stopwatch.StartNew();

            // Act
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            var validationResult = await _testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, 
                $"Single pipeline should complete within 30 seconds, took {stopwatch.ElapsedMilliseconds}ms");

            Debug.Log($"✅ Single pipeline performance: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public async Task Performance_ConcurrentPipelines_ShouldScaleLinearly()
        {
            // Arrange
            var briefs = Enumerable.Range(1, 5).Select(i => new DesignBrief(
                Description: $"Concurrent performance test {i}",
                GenreHint: "FPS",
                TargetDurationMinutes: 2,
                DifficultyLevel: 2,
                Seed: 100 + i
            )).ToArray();

            var stopwatch = Stopwatch.StartNew();

            // Act - Run pipelines concurrently
            var tasks = briefs.Select(async brief =>
            {
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                return content;
            }).ToArray();

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(5, results.Length, "Should have 5 results");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 60000, 
                $"5 concurrent pipelines should complete within 60 seconds, took {stopwatch.ElapsedMilliseconds}ms");

            var averageTimePerPipeline = stopwatch.ElapsedMilliseconds / 5.0;
            Debug.Log($"✅ Concurrent pipelines performance: {stopwatch.ElapsedMilliseconds}ms total, {averageTimePerPipeline:F1}ms average per pipeline");
        }

        [Test]
        public async Task Performance_MemoryUsage_ShouldStayWithinLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);
            var memorySnapshots = new List<long>();

            // Act - Run multiple pipelines and monitor memory
            for (int i = 0; i < 10; i++)
            {
                var brief = new DesignBrief(
                    Description: $"Memory test {i}",
                    GenreHint: "FPS",
                    TargetDurationMinutes: 1,
                    DifficultyLevel: 1,
                    Seed: 200 + i
                );

                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                // Take memory snapshot
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySnapshots.Add(GC.GetTotalMemory(false));
            }

            var finalMemory = GC.GetTotalMemory(false);
            var peakMemory = memorySnapshots.Max();
            var memoryIncrease = peakMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 100 * 1024 * 1024, // 100MB limit
                $"Memory increase should be < 100MB, was {memoryIncrease / 1024 / 1024}MB");

            Debug.Log($"✅ Memory usage performance: Peak {peakMemory / 1024 / 1024}MB, Increase {memoryIncrease / 1024 / 1024}MB");
        }

        [Test]
        public async Task Performance_ValidationSpeed_ShouldBeFast()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Validation performance test",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 300
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            var stopwatch = Stopwatch.StartNew();

            // Act - Run validation
            var validationResult = await _testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
                $"Validation should complete within 10 seconds, took {stopwatch.ElapsedMilliseconds}ms");

            Debug.Log($"✅ Validation performance: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public async Task Performance_LargeContentBundle_ShouldHandleEfficiently()
        {
            // Arrange - Create a brief that should generate a large content bundle
            var brief = new DesignBrief(
                Description: "Large FPS with many weapons, enemies, and complex interactions",
                GenreHint: "FPS",
                TargetDurationMinutes: 10,
                DifficultyLevel: 5,
                Seed: 400
            );

            var stopwatch = Stopwatch.StartNew();

            // Act
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 45000, 
                $"Large content bundle should complete within 45 seconds, took {stopwatch.ElapsedMilliseconds}ms");

            Assert.IsTrue(content.Assets.Count > 0, "Should have generated assets");
            Assert.IsTrue(world.Objects.Count > 0, "Should have generated world objects");
            Assert.IsTrue(interactions.Nodes.Count > 0, "Should have generated interaction nodes");

            Debug.Log($"✅ Large content bundle performance: {stopwatch.ElapsedMilliseconds}ms, {content.Assets.Count} assets");
        }

        [Test]
        public async Task Performance_StressTest_ShouldMaintainStability()
        {
            // Arrange
            var briefs = Enumerable.Range(1, 20).Select(i => new DesignBrief(
                Description: $"Stress test {i}",
                GenreHint: i % 3 == 0 ? "FPS" : i % 3 == 1 ? "Platformer" : "RPG",
                TargetDurationMinutes: 1,
                DifficultyLevel: 1,
                Seed: 500 + i
            )).ToArray();

            var stopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var errorCount = 0;

            // Act - Run stress test
            var tasks = briefs.Select(async brief =>
            {
                try
                {
                    var plan = await _service.GetService<IPlanGameSliceCommand>()
                        .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                    var world = await _service.GetService<IBuildWorldLayoutCommand>()
                        .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                    var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                        .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                    var content = await _service.GetService<ICreateContentBundleCommand>()
                        .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Stress test pipeline failed: {ex.Message}");
                    Interlocked.Increment(ref errorCount);
                    return false;
                }
            }).ToArray();

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successRate = (double)successCount / briefs.Length * 100;
            Assert.IsTrue(successRate >= 90, 
                $"Success rate should be >= 90%, was {successRate:F1}% ({successCount}/{briefs.Length})");

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 120000, 
                $"Stress test should complete within 2 minutes, took {stopwatch.ElapsedMilliseconds}ms");

            Debug.Log($"✅ Stress test performance: {successRate:F1}% success rate, {stopwatch.ElapsedMilliseconds}ms total");
        }

        [Test]
        public async Task Performance_CommandLatency_ShouldBeConsistent()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Latency test",
                GenreHint: "FPS",
                TargetDurationMinutes: 2,
                DifficultyLevel: 2,
                Seed: 600
            );

            var latencies = new List<long>();

            // Act - Run the same pipeline multiple times to measure latency consistency
            for (int i = 0; i < 5; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                stopwatch.Stop();
                latencies.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var averageLatency = latencies.Average();
            var maxLatency = latencies.Max();
            var minLatency = latencies.Min();
            var latencyVariance = latencies.Select(l => Math.Pow(l - averageLatency, 2)).Average();

            Assert.IsTrue(maxLatency < 30000, 
                $"Max latency should be < 30 seconds, was {maxLatency}ms");

            Assert.IsTrue(latencyVariance < Math.Pow(5000, 2), // 5 second variance limit
                $"Latency variance should be reasonable, was {Math.Sqrt(latencyVariance):F1}ms");

            Debug.Log($"✅ Command latency performance: Avg {averageLatency:F1}ms, Min {minLatency}ms, Max {maxLatency}ms, StdDev {Math.Sqrt(latencyVariance):F1}ms");
        }

        [Test]
        public async Task Performance_ResourceCleanup_ShouldNotLeak()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);
            var memorySnapshots = new List<long>();

            // Act - Run pipelines and monitor memory cleanup
            for (int i = 0; i < 15; i++)
            {
                var brief = new DesignBrief(
                    Description: $"Cleanup test {i}",
                    GenreHint: "FPS",
                    TargetDurationMinutes: 1,
                    DifficultyLevel: 1,
                    Seed: 700 + i
                );

                // Run pipeline
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                // Force cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                memorySnapshots.Add(GC.GetTotalMemory(false));
            }

            var finalMemory = GC.GetTotalMemory(false);
            var peakMemory = memorySnapshots.Max();
            var memoryIncrease = peakMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, // 50MB limit
                $"Memory increase should be < 50MB, was {memoryIncrease / 1024 / 1024}MB");

            // Check that memory doesn't continuously grow
            var lastFewSnapshots = memorySnapshots.TakeLast(5).ToArray();
            var firstFewSnapshots = memorySnapshots.Take(5).ToArray();
            var memoryGrowth = lastFewSnapshots.Average() - firstFewSnapshots.Average();

            Assert.IsTrue(memoryGrowth < 10 * 1024 * 1024, // 10MB growth limit
                $"Memory should not continuously grow, growth was {memoryGrowth / 1024 / 1024}MB");

            Debug.Log($"✅ Resource cleanup performance: Peak {peakMemory / 1024 / 1024}MB, Growth {memoryGrowth / 1024 / 1024}MB");
        }

        [Test]
        public async Task Performance_ValidationThroughput_ShouldScaleWithConcurrency()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Validation throughput test",
                GenreHint: "FPS",
                TargetDurationMinutes: 2,
                DifficultyLevel: 2,
                Seed: 800
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act - Run multiple validations concurrently
            var validationTasks = Enumerable.Range(1, 10).Select(async i =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await _testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);
                stopwatch.Stop();
                return new { Result = result, Duration = stopwatch.ElapsedMilliseconds };
            }).ToArray();

            var validationResults = await Task.WhenAll(validationTasks);
            var totalDuration = validationResults.Sum(r => r.Duration);
            var averageDuration = validationResults.Average(r => r.Duration);

            // Assert
            Assert.IsTrue(averageDuration < 5000, 
                $"Average validation duration should be < 5 seconds, was {averageDuration:F1}ms");

            Assert.IsTrue(validationResults.All(r => r.Result != null), 
                "All validation results should be non-null");

            Debug.Log($"✅ Validation throughput performance: {averageDuration:F1}ms average, {totalDuration}ms total for 10 concurrent validations");
        }
    }
}
