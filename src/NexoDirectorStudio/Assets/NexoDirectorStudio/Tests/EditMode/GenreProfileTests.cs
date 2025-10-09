using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for genre profile integration with commands
    /// </summary>
    public class GenreProfileTests
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
        public async Task Integration_GenreProfiles_ShouldWorkWithCommands()
        {
            // Arrange
            var genreRegistry = _service.GetService<GenreRegistry>();
            var profiles = new[] { "FPS", "Platformer", "RPG" };

            foreach (var genre in profiles)
            {
                var brief = new DesignBrief(
                    Description: $"Test {genre} game",
                    GenreHint: genre,
                    TargetDurationMinutes: 3,
                    DifficultyLevel: 3,
                    Seed: 456
                );

                // Act
                var plan = await _service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await _service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await _service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                // Assert
                Assert.AreEqual(genre, plan.Genre, $"Plan should have correct genre: {genre}");
                Assert.IsTrue(plan.CoreMechanics.Count > 0, $"Plan should have core mechanics for {genre}");
                Assert.IsTrue(plan.RequiredAssets.Count > 0, $"Plan should have required assets for {genre}");

                // Validate genre-specific requirements
                var profile = genreRegistry.GetProfile(genre);
                Assert.IsNotNull(profile, $"Profile should exist for {genre}");

                Debug.Log($"✅ Genre profile integration successful for {genre}");
            }
        }
    }
}
