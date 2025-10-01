using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests for genre profile detection and auto-detection functionality.
    /// </summary>
    [TestFixture]
    public class Unit_ProfileDetection
    {
        private GenreRegistry _registry;
        private FPSProfile _fpsProfile;
        private PlatformerProfile _platformerProfile;
        private RPGProfile _rpgProfile;
        
        [SetUp]
        public void SetUp()
        {
            _registry = new GenreRegistry();
            _fpsProfile = new FPSProfile();
            _platformerProfile = new PlatformerProfile();
            _rpgProfile = new RPGProfile();
            
            _registry.RegisterProfile(_fpsProfile);
            _registry.RegisterProfile(_platformerProfile);
            _registry.RegisterProfile(_rpgProfile);
        }
        
        [Test]
        public void FPSProfile_ShouldDetectFPSKeywords()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A fast-paced shooter with guns and combat",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            
            // Act
            var confidence = _fpsProfile.DetectGenre(brief);
            
            // Assert
            Assert.IsTrue(confidence >= 60, "FPS profile should detect FPS keywords with high confidence");
            Assert.IsTrue(confidence <= 100, "Confidence should not exceed 100");
        }
        
        [Test]
        public void PlatformerProfile_ShouldDetectPlatformerKeywords()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A jumping game with platforms and precise movement",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var confidence = _platformerProfile.DetectGenre(brief);
            
            // Assert
            Assert.IsTrue(confidence >= 60, "Platformer profile should detect platformer keywords with high confidence");
            Assert.IsTrue(confidence <= 100, "Confidence should not exceed 100");
        }
        
        [Test]
        public void RPGProfile_ShouldDetectRPGKeywords()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A role-playing game with quests and character development",
                GenreHint: "RPG",
                TargetDurationMinutes: 10,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var confidence = _rpgProfile.DetectGenre(brief);
            
            // Assert
            Assert.IsTrue(confidence >= 60, "RPG profile should detect RPG keywords with high confidence");
            Assert.IsTrue(confidence <= 100, "Confidence should not exceed 100");
        }
        
        [Test]
        public void GenreRegistry_ShouldAutoDetectBestProfile()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A fast-paced shooter with guns and combat",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            
            // Act
            var detectedProfile = _registry.AutoDetectGenre(brief);
            
            // Assert
            Assert.IsNotNull(detectedProfile, "Should detect a profile");
            Assert.AreEqual("FPS", detectedProfile.Name, "Should detect FPS profile");
        }
        
        [Test]
        public void GenreRegistry_ShouldReturnNullForLowConfidence()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A completely unrelated game description",
                GenreHint: "Unknown",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var detectedProfile = _registry.AutoDetectGenre(brief);
            
            // Assert
            Assert.IsNull(detectedProfile, "Should return null for low confidence");
        }
        
        [Test]
        public void GenreRegistry_ShouldGetMatchingProfiles()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A game with jumping and shooting mechanics",
                GenreHint: "Mixed",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var matches = _registry.GetMatchingProfiles(brief, 30);
            
            // Assert
            Assert.IsTrue(matches.Count > 0, "Should find matching profiles");
            Assert.IsTrue(matches.Count >= 2, "Should find multiple matching profiles");
            Assert.IsTrue(matches[0].Confidence >= matches[1].Confidence, "Should be ordered by confidence");
        }
        
        [Test]
        public void GenreRegistry_ShouldGetProfilesByKeywords()
        {
            // Arrange
            var keywords = new[] { "shoot", "gun", "combat" };
            
            // Act
            var profiles = _registry.GetProfilesByKeywords(keywords);
            
            // Assert
            Assert.IsTrue(profiles.Count > 0, "Should find profiles matching keywords");
            Assert.IsTrue(profiles.Any(p => p.Name == "First-Person Shooter"), "Should find FPS profile");
        }
        
        [Test]
        public void GenreRegistry_ShouldGetProfilesByMechanics()
        {
            // Arrange
            var mechanics = new[] { "Jump", "Run", "Move" };
            
            // Act
            var profiles = _registry.GetProfilesByMechanics(mechanics);
            
            // Assert
            Assert.IsTrue(profiles.Count > 0, "Should find profiles matching mechanics");
            Assert.IsTrue(profiles.Any(p => p.Name == "Platformer"), "Should find Platformer profile");
        }
        
        [Test]
        public void GenreRegistry_ShouldRegisterAndRetrieveProfiles()
        {
            // Arrange
            var customProfile = new FPSProfile();
            
            // Act
            _registry.RegisterProfile(customProfile);
            var retrievedProfile = _registry.GetProfileById("fps");
            
            // Assert
            Assert.IsNotNull(retrievedProfile, "Should retrieve registered profile");
            Assert.AreEqual("fps", retrievedProfile.Id, "Should have correct ID");
        }
        
        [Test]
        public void GenreRegistry_ShouldGetProfileByName()
        {
            // Act
            var profile = _registry.GetProfileByName("First-Person Shooter");
            
            // Assert
            Assert.IsNotNull(profile, "Should retrieve profile by name");
            Assert.AreEqual("First-Person Shooter", profile.Name, "Should have correct name");
        }
        
        [Test]
        public void GenreRegistry_ShouldHandleInvalidIds()
        {
            // Act
            var profile = _registry.GetProfileById("nonexistent");
            
            // Assert
            Assert.IsNull(profile, "Should return null for invalid ID");
        }
        
        [Test]
        public void GenreRegistry_ShouldHandleInvalidNames()
        {
            // Act
            var profile = _registry.GetProfileByName("Nonexistent Profile");
            
            // Assert
            Assert.IsNull(profile, "Should return null for invalid name");
        }
        
        [Test]
        public void GenreRegistry_ShouldGetSummary()
        {
            // Act
            var summary = _registry.GetSummary();
            
            // Assert
            Assert.IsNotNull(summary, "Should return summary");
            Assert.IsTrue(summary.Contains("Total Profiles: 3"), "Should contain profile count");
            Assert.IsTrue(summary.Contains("fps"), "Should contain profile IDs");
            Assert.IsTrue(summary.Contains("First-Person Shooter"), "Should contain profile names");
        }
        
        [Test]
        public void GenreRegistry_ShouldRemoveProfiles()
        {
            // Act
            var removed = _registry.RemoveProfile("fps");
            
            // Assert
            Assert.IsTrue(removed, "Should remove profile successfully");
            Assert.IsNull(_registry.GetProfileById("fps"), "Should not find removed profile");
        }
        
        [Test]
        public void GenreRegistry_ShouldClearAllProfiles()
        {
            // Act
            _registry.Clear();
            
            // Assert
            Assert.AreEqual(0, _registry.ProfileCount, "Should have no profiles after clear");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("fps", _fpsProfile.Id, "Should have correct ID");
            Assert.AreEqual("First-Person Shooter", _fpsProfile.Name, "Should have correct name");
            Assert.IsTrue(_fpsProfile.Keywords.Count > 0, "Should have keywords");
            Assert.IsTrue(_fpsProfile.CoreMechanics.Count > 0, "Should have core mechanics");
            Assert.IsNotNull(_fpsProfile.PerformanceBudgets, "Should have performance budgets");
            Assert.IsNotNull(_fpsProfile.PacingConfiguration, "Should have pacing configuration");
            Assert.IsNotNull(_fpsProfile.AccessibilityDefaults, "Should have accessibility defaults");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("platformer", _platformerProfile.Id, "Should have correct ID");
            Assert.AreEqual("Platformer", _platformerProfile.Name, "Should have correct name");
            Assert.IsTrue(_platformerProfile.Keywords.Count > 0, "Should have keywords");
            Assert.IsTrue(_platformerProfile.CoreMechanics.Count > 0, "Should have core mechanics");
            Assert.IsNotNull(_platformerProfile.PerformanceBudgets, "Should have performance budgets");
            Assert.IsNotNull(_platformerProfile.PacingConfiguration, "Should have pacing configuration");
            Assert.IsNotNull(_platformerProfile.AccessibilityDefaults, "Should have accessibility defaults");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveCorrectProperties()
        {
            // Assert
            Assert.AreEqual("rpg", _rpgProfile.Id, "Should have correct ID");
            Assert.AreEqual("Role-Playing Game", _rpgProfile.Name, "Should have correct name");
            Assert.IsTrue(_rpgProfile.Keywords.Count > 0, "Should have keywords");
            Assert.IsTrue(_rpgProfile.CoreMechanics.Count > 0, "Should have core mechanics");
            Assert.IsNotNull(_rpgProfile.PerformanceBudgets, "Should have performance budgets");
            Assert.IsNotNull(_rpgProfile.PacingConfiguration, "Should have pacing configuration");
            Assert.IsNotNull(_rpgProfile.AccessibilityDefaults, "Should have accessibility defaults");
        }
        
        [Test]
        public void FPSProfile_ShouldDetectGenreWithNullBrief()
        {
            // Act
            var confidence = _fpsProfile.DetectGenre(null);
            
            // Assert
            Assert.AreEqual(0, confidence, "Should return 0 confidence for null brief");
        }
        
        [Test]
        public void PlatformerProfile_ShouldDetectGenreWithEmptyDescription()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "",
                GenreHint: "",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var confidence = _platformerProfile.DetectGenre(brief);
            
            // Assert
            Assert.AreEqual(0, confidence, "Should return 0 confidence for empty description");
        }
        
        [Test]
        public void RPGProfile_ShouldDetectGenreWithMixedKeywords()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "A game with jumping, shooting, and quest mechanics",
                GenreHint: "Mixed",
                TargetDurationMinutes: 8,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var fpsConfidence = _fpsProfile.DetectGenre(brief);
            var platformerConfidence = _platformerProfile.DetectGenre(brief);
            var rpgConfidence = _rpgProfile.DetectGenre(brief);
            
            // Assert
            Assert.IsTrue(fpsConfidence > 0, "Should detect some FPS confidence");
            Assert.IsTrue(platformerConfidence > 0, "Should detect some platformer confidence");
            Assert.IsTrue(rpgConfidence > 0, "Should detect some RPG confidence");
        }
    }
}
