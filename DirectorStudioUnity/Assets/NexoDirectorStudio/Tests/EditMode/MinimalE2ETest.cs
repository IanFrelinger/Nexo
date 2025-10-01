using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NUnit.Framework;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Minimal E2E test to verify basic functionality works.
    /// </summary>
    [TestFixture]
    public class MinimalE2ETest : IDisposable
    {
        private DirectorStudioService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
        }

        [TearDown]
        public void Dispose()
        {
            _service?.Dispose();
        }

        [Test]
        public void Service_ShouldBeInitialized()
        {
            // Assert
            Assert.IsNotNull(_service);
        }

        [Test]
        public void Service_ShouldProvideCommands()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IPlanGameSliceCommand>());
            Assert.IsNotNull(_service.GetService<IBuildWorldLayoutCommand>());
            Assert.IsNotNull(_service.GetService<IPlaceInteractionsCommand>());
            Assert.IsNotNull(_service.GetService<ICreateContentBundleCommand>());
        }

        [Test]
        public async Task BasicWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1
            );

            // Act - Plan Game Slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);

            // Assert - Game Plan
            Assert.IsNotNull(gamePlan);
            Assert.IsNotNull(gamePlan.Id);
            Assert.IsNotNull(gamePlan.Description);
            Assert.IsTrue(gamePlan.CoreMechanics.Count >= 0);
            Assert.IsTrue(gamePlan.RequiredAssets.Count >= 0);

            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);

            // Assert - World Layout
            Assert.IsNotNull(worldLayout);
            Assert.IsNotNull(worldLayout.Id);
            Assert.IsTrue(worldLayout.Dimensions.x > 0 && worldLayout.Dimensions.y > 0);

            // Act - Place Interactions
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);

            // Assert - Interaction Graph
            Assert.IsNotNull(interactionGraph);
            Assert.IsNotNull(interactionGraph.Id);
            Assert.IsTrue(interactionGraph.Nodes.Count >= 0);

            // Act - Create Content Bundle
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);

            // Assert - Content Bundle
            Assert.IsNotNull(contentBundle);
            Assert.IsNotNull(contentBundle.Id);
        }

        [Test]
        public void Adapters_ShouldBeAvailable()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IOllamaAdapter>());
            Assert.IsNotNull(_service.GetService<ITextureGenAdapter>());
            Assert.IsNotNull(_service.GetService<ITtsAdapter>());
        }

        [Test]
        public async Task AdapterHealthChecks_ShouldWork()
        {
            // Act
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();

            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(ollamaHealth.IsHealthy);
            Assert.IsTrue(comfyuiHealth.IsHealthy);
            Assert.IsTrue(piperHealth.IsHealthy);

            Assert.IsNotNull(ollamaHealth.Message);
            Assert.IsNotNull(comfyuiHealth.Message);
            Assert.IsNotNull(piperHealth.Message);
        }
    }
}
