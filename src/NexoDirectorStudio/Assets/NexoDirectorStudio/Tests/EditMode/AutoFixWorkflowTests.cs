using System;
using System.Collections.Generic;
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
    /// Tests for auto-fix workflow integration
    /// </summary>
    public class AutoFixWorkflowTests
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
        public async Task Integration_AutoFixWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Auto-fix integration test",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 3,
                Seed: 101112
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act - Run auto-fix workflow
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();

            // Create a mock validation report with issues
            var validationReport = new ValidationReport
            {
                ReportId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                Results = new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        IsValid = false,
                        ValidatorName = "TestValidator",
                        Message = "Test validation issue",
                        Issues = new List<ValidationIssue>
                        {
                            new ValidationIssue
                            {
                                Severity = ValidationSeverity.Warning,
                                Category = "Test",
                                Title = "Test Issue",
                                Description = "Test issue description",
                                Location = "Test.Location",
                                SuggestedFix = "Test fix suggestion"
                            }
                        }
                    }
                }
            };

            var autoFixes = await proposeFixesCommand.ExecuteAsync(
                new IProposeAutoFixesCommand.Input(validationReport), CancellationToken.None);

            // Assert
            Assert.IsNotNull(autoFixes, "Auto-fixes should be proposed");
            Assert.IsTrue(autoFixes.Suggestions.Count > 0, "Should have auto-fix suggestions");

            // Test applying fixes (this would normally modify the content)
            var applyResult = await applyFixesCommand.ExecuteAsync(
                new IApplyAutoFixesCommand.Input(autoFixes), CancellationToken.None);

            Assert.IsNotNull(applyResult, "Apply fixes result should not be null");

            Debug.Log($"✅ Auto-fix workflow integration successful - {autoFixes.Suggestions.Count} suggestions proposed");
        }
    }
}
