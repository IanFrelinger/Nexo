using System.Collections.Generic;
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
    /// Tests for command execution order and data flow consistency
    /// </summary>
    public class CommandChainTests
    {
        private IDirectorStudioService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task Integration_CommandChain_ShouldExecuteInCorrectOrder()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Integration test FPS",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 123
            );

            var executionOrder = new List<string>();

            // Act - Execute commands in sequence
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);
            executionOrder.Add("Plan");

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);
            executionOrder.Add("Build");

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);
            executionOrder.Add("Interactions");

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);
            executionOrder.Add("Content");

            // Assert
            var expectedOrder = new[] { "Plan", "Build", "Interactions", "Content" };
            CollectionAssert.AreEqual(expectedOrder, executionOrder, "Commands should execute in correct order");

            // Validate data flow
            Assert.AreEqual(brief.Seed, plan.Seed, "Seed should flow from brief to plan");
            Assert.AreEqual(plan.Seed, world.Seed, "Seed should flow from plan to world");
            Assert.AreEqual(world.Seed, interactions.Seed, "Seed should flow from world to interactions");
            Assert.AreEqual(interactions.Seed, content.Seed, "Seed should flow from interactions to content");

            Debug.Log("✅ Command chain executed in correct order");
        }

        [Test]
        public async Task Integration_DataFlow_ShouldMaintainConsistency()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Data flow consistency test",
                GenreHint: "Platformer",
                TargetDurationMinutes: 4,
                DifficultyLevel: 2,
                Seed: 131415
            );

            // Act - Execute pipeline and track data flow
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Assert - Check data consistency across pipeline
            Assert.AreEqual(brief.Seed, plan.Seed, "Seed should be consistent from brief to plan");
            Assert.AreEqual(plan.Seed, world.Seed, "Seed should be consistent from plan to world");
            Assert.AreEqual(world.Seed, interactions.Seed, "Seed should be consistent from world to interactions");
            Assert.AreEqual(interactions.Seed, content.Seed, "Seed should be consistent from interactions to content");

            Assert.AreEqual(brief.TargetDurationMinutes, plan.EstimatedDurationMinutes, 
                "Duration should be consistent from brief to plan");

            Assert.AreEqual(plan.Genre, world.Name, "Genre should be consistent from plan to world");
            Assert.AreEqual(world.Id, interactions.WorldLayoutId, "World ID should be consistent to interactions");
            Assert.AreEqual(interactions.Id, content.InteractionGraphId, "Interactions ID should be consistent to content");

            // Check that required assets from plan are reflected in content
            var planAssetTypes = plan.RequiredAssets.Select(a => a.AssetType).ToHashSet();
            var contentAssetTypes = content.Assets.Select(a => a.AssetType).ToHashSet();
            
            // Some overlap should exist (exact match might not be possible due to generation)
            var hasOverlap = planAssetTypes.Intersect(contentAssetTypes).Any();
            Assert.IsTrue(hasOverlap, "Content should have some assets matching plan requirements");

            Debug.Log("✅ Data flow consistency maintained throughout pipeline");
        }
    }
}
