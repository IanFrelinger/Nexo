using System;
using System.Collections;
using System.Collections.Generic;
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
using NexoDirectorStudio.Agents;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// End-to-end smoke tests for the NexoDirector pipeline.
    /// Tests the complete flow from natural language input to validated game components.
    /// </summary>
    public class E2ESmokeTests
    {
        private IDirectorStudioService _service;
        private TestRunnerIntegration _testRunner;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
            _testRunner = new TestRunnerIntegration(maxRetries: 2, timeoutSeconds: 20f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task E2E_FPS_Pipeline_ShouldCompleteSuccessfully()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Doom-style FPS with intense combat, multiple weapons, and enemy AI",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );

            // Act - Run complete pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsNotNull(pipelineResult, "Pipeline result should not be null");
            Assert.IsTrue(pipelineResult.Success, $"Pipeline should succeed: {pipelineResult.ErrorMessage}");
            Assert.IsNotNull(pipelineResult.GamePlan, "Game plan should be generated");
            Assert.IsNotNull(pipelineResult.WorldLayout, "World layout should be generated");
            Assert.IsNotNull(pipelineResult.InteractionGraph, "Interaction graph should be generated");
            Assert.IsNotNull(pipelineResult.ContentBundle, "Content bundle should be generated");
            Assert.IsNotNull(pipelineResult.ValidationResult, "Validation result should be present");

            // Validate game plan
            Assert.AreEqual("FPS", pipelineResult.GamePlan.Genre, "Genre should be FPS");
            Assert.IsTrue(pipelineResult.GamePlan.CoreMechanics.Count > 0, "Core mechanics should be present");
            Assert.IsTrue(pipelineResult.GamePlan.RequiredAssets.Count > 0, "Required assets should be present");

            // Validate world layout
            Assert.IsTrue(pipelineResult.WorldLayout.Dimensions.x > 0, "World should have positive dimensions");
            Assert.IsTrue(pipelineResult.WorldLayout.Tiles.Count > 0, "World should have tiles");
            Assert.IsTrue(pipelineResult.WorldLayout.Objects.Count > 0, "World should have objects");

            // Validate interaction graph
            Assert.IsTrue(pipelineResult.InteractionGraph.Nodes.Count > 0, "Interaction graph should have nodes");
            Assert.IsTrue(pipelineResult.InteractionGraph.Connections.Count > 0, "Interaction graph should have connections");

            // Validate content bundle
            Assert.IsTrue(pipelineResult.ContentBundle.Assets.Count > 0, "Content bundle should have assets");

            // Validate validation results
            Assert.IsTrue(pipelineResult.ValidationResult.OverallPassed, 
                $"Validation should pass: {pipelineResult.ValidationResult.ValidationError}");
            Assert.IsTrue(pipelineResult.ValidationResult.OverallScore >= 70f, 
                $"Validation score should be >= 70%: {pipelineResult.ValidationResult.OverallScore}%");

            Debug.Log($"✅ E2E FPS Pipeline completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Platformer_Pipeline_ShouldCompleteSuccessfully()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Mario-style platformer with jumping mechanics, collectibles, and moving platforms",
                GenreHint: "Platformer",
                TargetDurationMinutes: 3,
                DifficultyLevel: 2,
                Seed: 54321
            );

            // Act - Run complete pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsNotNull(pipelineResult, "Pipeline result should not be null");
            Assert.IsTrue(pipelineResult.Success, $"Pipeline should succeed: {pipelineResult.ErrorMessage}");
            Assert.AreEqual("Platformer", pipelineResult.GamePlan.Genre, "Genre should be Platformer");
            Assert.IsTrue(pipelineResult.ValidationResult.OverallPassed, "Validation should pass");

            Debug.Log($"✅ E2E Platformer Pipeline completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_RPG_Pipeline_ShouldCompleteSuccessfully()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Fantasy RPG with NPCs, quests, dialogue system, and character progression",
                GenreHint: "RPG",
                TargetDurationMinutes: 8,
                DifficultyLevel: 3,
                Seed: 98765
            );

            // Act - Run complete pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsNotNull(pipelineResult, "Pipeline result should not be null");
            Assert.IsTrue(pipelineResult.Success, $"Pipeline should succeed: {pipelineResult.ErrorMessage}");
            Assert.AreEqual("RPG", pipelineResult.GamePlan.Genre, "Genre should be RPG");
            Assert.IsTrue(pipelineResult.ValidationResult.OverallPassed, "Validation should pass");

            Debug.Log($"✅ E2E RPG Pipeline completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Pipeline_WithRetryLogic_ShouldEventuallySucceed()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Complex FPS with advanced AI, multiple weapon types, and complex interactions",
                GenreHint: "FPS",
                TargetDurationMinutes: 10,
                DifficultyLevel: 5,
                Seed: 11111
            );

            // Act - Run pipeline with retry logic
            var pipelineResult = await RunCompletePipelineWithRetryAsync(brief, maxRetries: 3);

            // Assert
            Assert.IsNotNull(pipelineResult, "Pipeline result should not be null");
            Assert.IsTrue(pipelineResult.Success, $"Pipeline should eventually succeed: {pipelineResult.ErrorMessage}");
            Assert.IsTrue(pipelineResult.ValidationResult.OverallPassed, "Final validation should pass");
            Assert.IsTrue(pipelineResult.ValidationResult.OverallScore >= 60f, 
                "Final validation score should be reasonable even after retries");

            Debug.Log($"✅ E2E Pipeline with retry completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Pipeline_Performance_ShouldMeetThresholds()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Performance-optimized FPS for mobile devices with limited resources",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 2,
                Seed: 22222
            );

            var startTime = DateTimeOffset.UtcNow;

            // Act - Run pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);
            var endTime = DateTimeOffset.UtcNow;
            var totalDuration = endTime - startTime;

            // Assert
            Assert.IsTrue(pipelineResult.Success, "Pipeline should succeed");
            Assert.IsTrue(totalDuration.TotalSeconds < 60, 
                $"Pipeline should complete within 60 seconds, took {totalDuration.TotalSeconds:F1}s");

            // Check performance-specific validation
            var performanceSuite = pipelineResult.ValidationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "PerformanceTestSuite");
            Assert.IsNotNull(performanceSuite, "Performance test suite should be present");
            Assert.IsTrue(performanceSuite.Passed, "Performance tests should pass");

            Debug.Log($"✅ E2E Performance Pipeline completed in {totalDuration.TotalSeconds:F1}s - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Pipeline_Accessibility_ShouldMeetStandards()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Accessible platformer with high contrast, large text, and audio cues",
                GenreHint: "Platformer",
                TargetDurationMinutes: 4,
                DifficultyLevel: 1,
                Seed: 33333
            );

            // Act - Run pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsTrue(pipelineResult.Success, "Pipeline should succeed");

            // Check accessibility-specific validation
            var accessibilitySuite = pipelineResult.ValidationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "AccessibilityTestSuite");
            Assert.IsNotNull(accessibilitySuite, "Accessibility test suite should be present");
            Assert.IsTrue(accessibilitySuite.Passed, "Accessibility tests should pass");

            Debug.Log($"✅ E2E Accessibility Pipeline completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Pipeline_Integration_ShouldWorkCorrectly()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Complex RPG with integrated systems: combat, dialogue, inventory, and quests",
                GenreHint: "RPG",
                TargetDurationMinutes: 6,
                DifficultyLevel: 4,
                Seed: 44444
            );

            // Act - Run pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsTrue(pipelineResult.Success, "Pipeline should succeed");

            // Check integration-specific validation
            var integrationSuite = pipelineResult.ValidationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "IntegrationTestSuite");
            Assert.IsNotNull(integrationSuite, "Integration test suite should be present");
            Assert.IsTrue(integrationSuite.Passed, "Integration tests should pass");

            // Validate that components can interact with each other
            Assert.IsTrue(pipelineResult.InteractionGraph.Nodes.Count > 0, "Should have interaction nodes");
            Assert.IsTrue(pipelineResult.InteractionGraph.Connections.Count > 0, "Should have interaction connections");

            Debug.Log($"✅ E2E Integration Pipeline completed successfully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
        }

        [Test]
        public async Task E2E_Pipeline_ErrorHandling_ShouldHandleFailuresGracefully()
        {
            // Arrange - Create a brief that might cause issues
            var brief = new DesignBrief(
                Description: "", // Empty description might cause issues
                GenreHint: "UnknownGenre", // Unknown genre
                TargetDurationMinutes: -1, // Invalid duration
                DifficultyLevel: 10, // Invalid difficulty
                Seed: 55555
            );

            // Act - Run pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Assert - Should either succeed with defaults or fail gracefully
            if (pipelineResult.Success)
            {
                Assert.IsNotNull(pipelineResult.GamePlan, "Should generate a game plan even with invalid input");
                Debug.Log($"✅ E2E Error Handling Pipeline handled invalid input gracefully - Score: {pipelineResult.ValidationResult.OverallScore:F1}%");
            }
            else
            {
                Assert.IsNotNull(pipelineResult.ErrorMessage, "Should provide error message for failure");
                Debug.Log($"✅ E2E Error Handling Pipeline failed gracefully: {pipelineResult.ErrorMessage}");
            }
        }

        [Test]
        public async Task E2E_Pipeline_ConcurrentExecution_ShouldWorkCorrectly()
        {
            // Arrange - Create multiple briefs for concurrent execution
            var briefs = new[]
            {
                new DesignBrief("FPS game", "FPS", 3, 3, 111),
                new DesignBrief("Platformer game", "Platformer", 2, 2, 222),
                new DesignBrief("RPG game", "RPG", 4, 3, 333)
            };

            // Act - Run pipelines concurrently
            var tasks = briefs.Select(brief => RunCompletePipelineAsync(brief)).ToArray();
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(3, results.Length, "Should have 3 results");
            
            foreach (var result in results)
            {
                Assert.IsNotNull(result, "Each result should not be null");
                Assert.IsTrue(result.Success, $"Each pipeline should succeed: {result.ErrorMessage}");
            }

            Debug.Log($"✅ E2E Concurrent Execution completed successfully - {results.Length} pipelines");
        }

        [Test]
        public async Task E2E_Pipeline_MemoryUsage_ShouldStayWithinLimits()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Memory-intensive FPS with many objects and complex interactions",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 66666
            );

            var initialMemory = GC.GetTotalMemory(false);

            // Act - Run pipeline
            var pipelineResult = await RunCompletePipelineAsync(brief);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(pipelineResult.Success, "Pipeline should succeed");
            Assert.IsTrue(memoryIncrease < 100 * 1024 * 1024, // 100MB limit
                $"Memory increase should be < 100MB, was {memoryIncrease / 1024 / 1024}MB");

            Debug.Log($"✅ E2E Memory Usage Pipeline completed - Memory increase: {memoryIncrease / 1024 / 1024}MB");
        }

        [Test]
        public async Task E2E_Pipeline_DeterministicGeneration_ShouldBeReproducible()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Deterministic FPS for testing reproducibility",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 77777
            );

            // Act - Run pipeline twice with same seed
            var result1 = await RunCompletePipelineAsync(brief);
            var result2 = await RunCompletePipelineAsync(brief);

            // Assert
            Assert.IsTrue(result1.Success, "First run should succeed");
            Assert.IsTrue(result2.Success, "Second run should succeed");

            // Compare key properties for determinism
            Assert.AreEqual(result1.GamePlan.Genre, result2.GamePlan.Genre, "Genre should be identical");
            Assert.AreEqual(result1.GamePlan.CoreMechanics.Count, result2.GamePlan.CoreMechanics.Count, "Core mechanics count should be identical");
            Assert.AreEqual(result1.WorldLayout.Dimensions, result2.WorldLayout.Dimensions, "World dimensions should be identical");
            Assert.AreEqual(result1.InteractionGraph.Nodes.Count, result2.InteractionGraph.Nodes.Count, "Interaction nodes count should be identical");

            Debug.Log($"✅ E2E Deterministic Generation Pipeline completed - Results are reproducible");
        }

        /// <summary>
        /// Runs the complete NexoDirector pipeline from brief to validated content.
        /// </summary>
        private async Task<PipelineResult> RunCompletePipelineAsync(DesignBrief brief)
        {
            try
            {
                // Step 1: Plan Game Slice
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                // Step 2: Build World Layout
                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                // Step 3: Place Interactions
                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                // Step 4: Create Content Bundle
                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                // Step 5: Validate Components
                var validationResult = await _testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

                return new PipelineResult
                {
                    Success = true,
                    GamePlan = plan,
                    WorldLayout = world,
                    InteractionGraph = interactions,
                    ContentBundle = content,
                    ValidationResult = validationResult
                };
            }
            catch (Exception ex)
            {
                return new PipelineResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Runs the complete pipeline with retry logic for failed validations.
        /// </summary>
        private async Task<PipelineResult> RunCompletePipelineWithRetryAsync(DesignBrief brief, int maxRetries)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var result = await RunCompletePipelineAsync(brief);
                
                if (result.Success && result.ValidationResult.OverallPassed)
                {
                    return result;
                }

                if (attempt < maxRetries)
                {
                    Debug.Log($"🔄 Pipeline attempt {attempt} failed, retrying... (Score: {result.ValidationResult?.OverallScore:F1}%)");
                }
            }

            // Return the last result even if it failed
            return await RunCompletePipelineAsync(brief);
        }
    }

    /// <summary>
    /// Result of a complete pipeline execution.
    /// </summary>
    public class PipelineResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public GamePlan GamePlan { get; set; }
        public WorldLayout WorldLayout { get; set; }
        public InteractionGraph InteractionGraph { get; set; }
        public ContentBundle ContentBundle { get; set; }
        public ComponentValidationResult ValidationResult { get; set; }
    }
}
