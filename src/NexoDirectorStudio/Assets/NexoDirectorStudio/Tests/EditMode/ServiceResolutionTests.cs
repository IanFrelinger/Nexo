using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for service container resolution and dependency injection
    /// </summary>
    public class ServiceResolutionTests
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
        public async Task Integration_ServiceContainer_ShouldResolveAllServices()
        {
            // Act & Assert - Test that all services can be resolved
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();

            Assert.IsNotNull(planCommand, "Plan command should be resolvable");
            Assert.IsNotNull(buildCommand, "Build command should be resolvable");
            Assert.IsNotNull(interactionsCommand, "Interactions command should be resolvable");
            Assert.IsNotNull(contentCommand, "Content command should be resolvable");
            Assert.IsNotNull(proposeFixesCommand, "Propose fixes command should be resolvable");
            Assert.IsNotNull(applyFixesCommand, "Apply fixes command should be resolvable");

            // Test adapters
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyUIAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();

            Assert.IsNotNull(ollamaAdapter, "Ollama adapter should be resolvable");
            Assert.IsNotNull(comfyUIAdapter, "ComfyUI adapter should be resolvable");
            Assert.IsNotNull(piperAdapter, "Piper adapter should be resolvable");

            // Test validators
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            Assert.IsNotNull(validators, "Validators should be resolvable");
            Assert.IsTrue(validators.Any(), "Should have at least one validator");

            // Test genre profiles
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();

            Assert.IsNotNull(genreRegistry, "Genre registry should be resolvable");
            Assert.IsNotNull(genreProfileService, "Genre profile service should be resolvable");

            Debug.Log("✅ All services resolved successfully");
        }
    }
}
