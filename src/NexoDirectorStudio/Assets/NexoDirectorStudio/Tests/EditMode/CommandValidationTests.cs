using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for command execution validation
    /// </summary>
    public class CommandValidationTests : IDisposable
    {
        private readonly DirectorStudioService _service;
        
        public CommandValidationTests()
        {
            _service = new DirectorStudioService();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task Commands_ShouldBeExecutable()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 3,
                DifficultyLevel: 0.3f,
                Seed: 456
            );
            
            // Act & Assert - Plan Command
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            Assert.IsNotNull(planCommand, "Plan command should be available");
            
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            
            // Act & Assert - Build Command
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            Assert.IsNotNull(buildCommand, "Build command should be available");
            
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            Assert.IsNotNull(worldLayout, "World layout should be generated");
            
            // Act & Assert - Interactions Command
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            Assert.IsNotNull(interactionsCommand, "Interactions command should be available");
            
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            Assert.IsNotNull(interactionGraph, "Interaction graph should be generated");
            
            // Act & Assert - Content Command
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            Assert.IsNotNull(contentCommand, "Content command should be available");
            
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            Assert.IsNotNull(contentBundle, "Content bundle should be generated");
        }
    }
}
