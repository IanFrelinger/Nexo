using System;
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
    /// Tests for resource management and memory cleanup
    /// </summary>
    public class ResourceManagementTests
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
        public async Task Integration_ResourceCleanup_ShouldNotLeakMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Run multiple pipelines to test for memory leaks
            for (int i = 0; i < 5; i++)
            {
                var brief = new DesignBrief(
                    Description: $"Memory leak test {i}",
                    GenreHint: "FPS",
                    TargetDurationMinutes: 1,
                    DifficultyLevel: 1,
                    Seed: 220 + i
                );

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
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, // 50MB limit
                $"Memory increase should be < 50MB, was {memoryIncrease / 1024 / 1024}MB");

            Debug.Log($"✅ Resource cleanup successful - Memory increase: {memoryIncrease / 1024 / 1024}MB");
        }
    }
}
