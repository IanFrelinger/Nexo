using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for deterministic behavior validation
    /// </summary>
    public class DeterministicValidationTests : IDisposable
    {
        private readonly IDirectorStudioService _service;
        
        public DeterministicValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task System_ShouldBeDeterministic()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A deterministic test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 0.5f,
                Seed: 789
            );
            
            // Act - Generate same plan twice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Should be deterministic (same seed should produce same result)
            Assert.AreEqual(gamePlan1.Genre, gamePlan2.Genre, "Genre should be deterministic");
            Assert.AreEqual(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes, "Duration should be deterministic");
            Assert.AreEqual(gamePlan1.CoreMechanics.Length, gamePlan2.CoreMechanics.Length, "Mechanics count should be deterministic");
        }
    }
}
