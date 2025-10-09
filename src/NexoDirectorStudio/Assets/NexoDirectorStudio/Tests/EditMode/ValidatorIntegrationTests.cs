using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Validation;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for validator integration with generated content
    /// </summary>
    public class ValidatorIntegrationTests
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
        public async Task Integration_Validators_ShouldWorkWithGeneratedContent()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Validator integration test",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 789
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act - Run validators
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            var validationResults = new List<ValidationResult>();

            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(plan, CancellationToken.None);
                validationResults.Add(result);
            }

            // Assert
            Assert.IsTrue(validationResults.Count > 0, "Should have validation results");
            Assert.IsTrue(validationResults.All(r => r != null), "All validation results should be non-null");

            var passedValidations = validationResults.Count(r => r.IsValid);
            Assert.IsTrue(passedValidations > 0, "At least some validations should pass");

            Debug.Log($"✅ Validators integration successful - {passedValidations}/{validationResults.Count} passed");
        }
    }
}
