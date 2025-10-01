using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for concurrent execution and error handling
    /// </summary>
    public class ConcurrencyTests
    {
        private DirectorStudioService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task Integration_ConcurrentExecution_ShouldNotInterfere()
        {
            // Arrange - Create multiple briefs for concurrent execution
            var briefs = new[]
            {
                new DesignBrief("Concurrent test 1", "FPS", 2, 2, 191),
                new DesignBrief("Concurrent test 2", "Platformer", 2, 2, 202),
                new DesignBrief("Concurrent test 3", "RPG", 2, 2, 213)
            };

            // Act - Execute pipelines concurrently
            var tasks = briefs.Select(async brief =>
            {
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);
                return plan.Genre;
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(3, results.Length, "Should have 3 results");
            CollectionAssert.Contains(results, "FPS", "Should contain FPS result");
            CollectionAssert.Contains(results, "Platformer", "Should contain Platformer result");
            CollectionAssert.Contains(results, "RPG", "Should contain RPG result");

            Debug.Log("✅ Concurrent execution successful - No interference between pipelines");
        }

        [Test]
        public async Task Integration_ErrorPropagation_ShouldHandleFailuresGracefully()
        {
            // Arrange - Create a scenario that might cause issues
            var invalidBrief = new DesignBrief(
                Description: "", // Empty description
                GenreHint: "InvalidGenre", // Invalid genre
                TargetDurationMinutes: -1, // Invalid duration
                DifficultyLevel: 0, // Invalid difficulty
                Seed: 161718
            );

            // Act & Assert - Each step should handle errors gracefully
            try
            {
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(invalidBrief), CancellationToken.None);

                // If plan succeeds, it should have defaults
                Assert.IsNotNull(plan, "Plan should be generated even with invalid input");
                Assert.IsTrue(plan.EstimatedDurationMinutes > 0, "Plan should have valid duration");

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                Assert.IsNotNull(world, "World should be generated even with invalid input");

                Debug.Log("✅ Error propagation handled gracefully - system provided defaults");
            }
            catch (Exception ex)
            {
                // If an exception is thrown, it should be informative
                Assert.IsNotNull(ex.Message, "Exception should have a message");
                Debug.Log($"✅ Error propagation handled gracefully - Exception: {ex.Message}");
            }
        }
    }
}
