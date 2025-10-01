using System;
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
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Integration tests for NexoDirector components working together.
    /// Tests the interaction between different parts of the system.
    /// 
    /// This class serves as the main orchestrator for integration tests.
    /// Individual test categories are split into separate classes for better organization.
    /// </summary>
    public class IntegrationTests
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
        public async Task Integration_EndToEndPipeline_ShouldCompleteSuccessfully()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "End-to-end integration test",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 999
            );

            // Act - Execute complete pipeline
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Assert - Validate pipeline completion
            Assert.IsNotNull(plan, "Plan should be generated");
            Assert.IsNotNull(world, "World should be generated");
            Assert.IsNotNull(interactions, "Interactions should be generated");
            Assert.IsNotNull(content, "Content should be generated");

            // Validate data consistency
            Assert.AreEqual(brief.Seed, plan.Seed, "Seed should be consistent");
            Assert.AreEqual(plan.Seed, world.Seed, "Seed should flow through pipeline");
            Assert.AreEqual(world.Seed, interactions.Seed, "Seed should flow through pipeline");
            Assert.AreEqual(interactions.Seed, content.Seed, "Seed should flow through pipeline");

            Debug.Log("✅ End-to-end pipeline completed successfully");
        }

        [Test]
        public async Task Integration_AgentDirector_ShouldOrchestratePipeline()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Agent director orchestration test",
                GenreHint: "Platformer",
                TargetDurationMinutes: 2,
                DifficultyLevel: 2,
                Seed: 888
            );

            var agentDirector = new AgentDirector(_service);

            // Act
            var result = await agentDirector.RunAsync(brief, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result, "Agent director should return result");
            Assert.IsNotNull(result.GamePlan, "Should have game plan");
            Assert.IsNotNull(result.WorldLayout, "Should have world layout");
            Assert.IsNotNull(result.InteractionGraph, "Should have interaction graph");
            Assert.IsNotNull(result.ContentBundle, "Should have content bundle");

            Debug.Log("✅ Agent director orchestration successful");
        }
    }
}