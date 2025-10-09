using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for genre profile system validation
    /// </summary>
    public class GenreProfileValidationTests : IDisposable
    {
        private readonly IDirectorStudioService _service;
        
        public GenreProfileValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task GenreProfileSystem_ShouldWork()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert - Registry
            Assert.IsNotNull(genreRegistry, "Genre registry should be available");
            var allProfiles = genreRegistry.GetAllProfiles();
            Assert.IsTrue(allProfiles.Count >= 3, "Should have at least 3 genre profiles");
            
            // Test FPS Profile
            var fpsProfile = genreRegistry.GetProfile("fps");
            Assert.IsNotNull(fpsProfile, "FPS profile should be available");
            Assert.AreEqual("FPS", fpsProfile.Name, "FPS profile should have correct name");
            
            // Test Platformer Profile
            var platformerProfile = genreRegistry.GetProfile("platformer");
            Assert.IsNotNull(platformerProfile, "Platformer profile should be available");
            Assert.AreEqual("Platformer", platformerProfile.Name, "Platformer profile should have correct name");
            
            // Test RPG Profile
            var rpgProfile = genreRegistry.GetProfile("rpg");
            Assert.IsNotNull(rpgProfile, "RPG profile should be available");
            Assert.AreEqual("Role-Playing Game", rpgProfile.Name, "RPG profile should have correct name");
            
            // Test Auto-Detection
            var designBrief = new DesignBrief(
                Description: "A first-person shooter with weapons and enemies",
                GenreHint: "",
                TargetDurationMinutes: 10,
                DifficultyLevel: 0.7f,
                Seed: 789
            );
            
            var detectedGenre = genreRegistry.DetectGenre(designBrief);
            Assert.IsNotNull(detectedGenre, "Should detect genre from brief");
        }
    }
}
