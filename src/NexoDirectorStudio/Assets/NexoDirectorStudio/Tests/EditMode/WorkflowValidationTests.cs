using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for complete workflow validation
    /// </summary>
    public class WorkflowValidationTests : IDisposable
    {
        private readonly IDirectorStudioService _service;
        
        public WorkflowValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task CompleteWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple platformer level with a few jumps and a single enemy.",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 0.5f,
                Seed: 123
            );
            
            // Act - Plan Game Slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Game Plan
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            Assert.IsNotNull(gamePlan.Id, "Game plan should have ID");
            Assert.IsNotNull(gamePlan.Description, "Game plan should have description");
            Assert.IsTrue(gamePlan.CoreMechanics.Length > 0, "Game plan should have core mechanics");
            Assert.IsTrue(gamePlan.RequiredAssets.Length > 0, "Game plan should have required assets");
            
            // Act - Validate Game Plan
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            var validationReport = new ValidationReport();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationReport.AddResult(result);
            }
            
            // Assert - Validation
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Results.Count > 0, "Should have validation results");
            Assert.IsNotNull(validationReport.OverallStatus, "Should have overall status");
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert - World Layout
            Assert.IsNotNull(worldLayout, "World layout should be generated");
            Assert.IsNotNull(worldLayout.Id, "World layout should have ID");
            Assert.IsTrue(worldLayout.GridSize.X > 0 && worldLayout.GridSize.Y > 0, "World layout should have valid grid size");
            
            // Act - Place Interactions
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            
            // Assert - Interaction Graph
            Assert.IsNotNull(interactionGraph, "Interaction graph should be generated");
            Assert.IsNotNull(interactionGraph.Id, "Interaction graph should have ID");
            
            // Act - Create Content Bundle
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            
            // Assert - Content Bundle
            Assert.IsNotNull(contentBundle, "Content bundle should be generated");
            Assert.IsNotNull(contentBundle.Id, "Content bundle should have ID");
        }
    }
}
