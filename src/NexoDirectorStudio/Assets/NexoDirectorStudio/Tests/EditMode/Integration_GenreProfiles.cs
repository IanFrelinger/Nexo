using NUnit.Framework;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Integration tests for genre profiles and their integration with the Director Studio service.
    /// </summary>
    [TestFixture]
    public class Integration_GenreProfiles
    {
        private DirectorStudioService _service;
        private GenreProfileService _profileService;
        
        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
            _profileService = _service.GetService<GenreProfileService>();
            _profileService.InitializeProfiles();
        }
        
        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }
        
        [Test]
        public void GenreProfileService_ShouldInitializeProfiles()
        {
            // Act
            var profiles = _profileService.GetAllProfiles();
            
            // Assert
            Assert.IsTrue(profiles.Count >= 3, "Should have at least 3 profiles");
            Assert.IsTrue(profiles.Any(p => p.Name == "First-Person Shooter"), "Should have FPS profile");
            Assert.IsTrue(profiles.Any(p => p.Name == "Platformer"), "Should have Platformer profile");
            Assert.IsTrue(profiles.Any(p => p.Name == "Role-Playing Game"), "Should have RPG profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldAutoDetectFPS()
        {
            // Arrange
            var brief = new DesignBrief
            {
                Description = "A fast-paced shooter with guns and combat",
                GenreHint = "FPS",
                TargetDurationMinutes = 5,
                DifficultyLevel = 4,
                Seed = 12345
            };
            
            // Act
            var detectedProfile = _profileService.AutoDetectGenre(brief);
            
            // Assert
            Assert.IsNotNull(detectedProfile, "Should detect a profile");
            Assert.AreEqual("First-Person Shooter", detectedProfile.Name, "Should detect FPS profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldAutoDetectPlatformer()
        {
            // Arrange
            var brief = new DesignBrief
            {
                Description = "A jumping game with platforms and precise movement",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 3,
                Seed = 12345
            };
            
            // Act
            var detectedProfile = _profileService.AutoDetectGenre(brief);
            
            // Assert
            Assert.IsNotNull(detectedProfile, "Should detect a profile");
            Assert.AreEqual("Platformer", detectedProfile.Name, "Should detect Platformer profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldAutoDetectRPG()
        {
            // Arrange
            var brief = new DesignBrief
            {
                Description = "A role-playing game with quests and character development",
                GenreHint = "RPG",
                TargetDurationMinutes = 10,
                DifficultyLevel = 3,
                Seed = 12345
            };
            
            // Act
            var detectedProfile = _profileService.AutoDetectGenre(brief);
            
            // Assert
            Assert.IsNotNull(detectedProfile, "Should detect a profile");
            Assert.AreEqual("Role-Playing Game", detectedProfile.Name, "Should detect RPG profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldGetProfilesByKeywords()
        {
            // Arrange
            var keywords = new[] { "shoot", "gun", "combat" };
            
            // Act
            var profiles = _profileService.GetProfilesByKeywords(keywords);
            
            // Assert
            Assert.IsTrue(profiles.Count > 0, "Should find profiles matching keywords");
            Assert.IsTrue(profiles.Any(p => p.Name == "First-Person Shooter"), "Should find FPS profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldGetProfilesByMechanics()
        {
            // Arrange
            var mechanics = new[] { "Jump", "Run", "Move" };
            
            // Act
            var profiles = _profileService.GetProfilesByMechanics(mechanics);
            
            // Assert
            Assert.IsTrue(profiles.Count > 0, "Should find profiles matching mechanics");
            Assert.IsTrue(profiles.Any(p => p.Name == "Platformer"), "Should find Platformer profile");
        }
        
        [Test]
        public void GenreProfileService_ShouldGetProfileById()
        {
            // Act
            var fpsProfile = _profileService.GetProfileById("fps");
            var platformerProfile = _profileService.GetProfileById("platformer");
            var rpgProfile = _profileService.GetProfileById("rpg");
            
            // Assert
            Assert.IsNotNull(fpsProfile, "Should find FPS profile by ID");
            Assert.AreEqual("First-Person Shooter", fpsProfile.Name, "Should have correct FPS profile name");
            
            Assert.IsNotNull(platformerProfile, "Should find Platformer profile by ID");
            Assert.AreEqual("Platformer", platformerProfile.Name, "Should have correct Platformer profile name");
            
            Assert.IsNotNull(rpgProfile, "Should find RPG profile by ID");
            Assert.AreEqual("Role-Playing Game", rpgProfile.Name, "Should have correct RPG profile name");
        }
        
        [Test]
        public void GenreProfileService_ShouldGetProfileByName()
        {
            // Act
            var fpsProfile = _profileService.GetProfileByName("First-Person Shooter");
            var platformerProfile = _profileService.GetProfileByName("Platformer");
            var rpgProfile = _profileService.GetProfileByName("Role-Playing Game");
            
            // Assert
            Assert.IsNotNull(fpsProfile, "Should find FPS profile by name");
            Assert.AreEqual("fps", fpsProfile.Id, "Should have correct FPS profile ID");
            
            Assert.IsNotNull(platformerProfile, "Should find Platformer profile by name");
            Assert.AreEqual("platformer", platformerProfile.Id, "Should have correct Platformer profile ID");
            
            Assert.IsNotNull(rpgProfile, "Should find RPG profile by name");
            Assert.AreEqual("rpg", rpgProfile.Id, "Should have correct RPG profile ID");
        }
        
        [Test]
        public void GenreProfileService_ShouldHandleInvalidIds()
        {
            // Act
            var profile = _profileService.GetProfileById("nonexistent");
            
            // Assert
            Assert.IsNull(profile, "Should return null for invalid ID");
        }
        
        [Test]
        public void GenreProfileService_ShouldHandleInvalidNames()
        {
            // Act
            var profile = _profileService.GetProfileByName("Nonexistent Profile");
            
            // Assert
            Assert.IsNull(profile, "Should return null for invalid name");
        }
        
        [Test]
        public void GenreProfileService_ShouldGetSummary()
        {
            // Act
            var summary = _profileService.GetSummary();
            
            // Assert
            Assert.IsNotNull(summary, "Should return summary");
            Assert.IsTrue(summary.Contains("Total Profiles: 3"), "Should contain profile count");
            Assert.IsTrue(summary.Contains("fps"), "Should contain profile IDs");
            Assert.IsTrue(summary.Contains("First-Person Shooter"), "Should contain profile names");
        }
        
        [Test]
        public void FPSProfile_ShouldValidateGenreRequirements()
        {
            // Arrange
            var fpsProfile = _profileService.GetProfileById("fps");
            var gamePlan = CreateTestGamePlan();
            
            // Act
            var validationResult = fpsProfile.ValidateGenreRequirements(gamePlan);
            
            // Assert
            Assert.IsNotNull(validationResult, "Should return validation result");
            Assert.IsTrue(validationResult.Score >= 0, "Should have valid score");
            Assert.IsNotNull(validationResult.Issues, "Should have issues list");
            Assert.IsNotNull(validationResult.Suggestions, "Should have suggestions list");
        }
        
        [Test]
        public void PlatformerProfile_ShouldValidateGenreRequirements()
        {
            // Arrange
            var platformerProfile = _profileService.GetProfileById("platformer");
            var gamePlan = CreateTestGamePlan();
            
            // Act
            var validationResult = platformerProfile.ValidateGenreRequirements(gamePlan);
            
            // Assert
            Assert.IsNotNull(validationResult, "Should return validation result");
            Assert.IsTrue(validationResult.Score >= 0, "Should have valid score");
            Assert.IsNotNull(validationResult.Issues, "Should have issues list");
            Assert.IsNotNull(validationResult.Suggestions, "Should have suggestions list");
        }
        
        [Test]
        public void RPGProfile_ShouldValidateGenreRequirements()
        {
            // Arrange
            var rpgProfile = _profileService.GetProfileById("rpg");
            var gamePlan = CreateTestGamePlan();
            
            // Act
            var validationResult = rpgProfile.ValidateGenreRequirements(gamePlan);
            
            // Assert
            Assert.IsNotNull(validationResult, "Should return validation result");
            Assert.IsTrue(validationResult.Score >= 0, "Should have valid score");
            Assert.IsNotNull(validationResult.Issues, "Should have issues list");
            Assert.IsNotNull(validationResult.Suggestions, "Should have suggestions list");
        }
        
        [Test]
        public void GenreProfiles_ShouldHaveDifferentPerformanceBudgets()
        {
            // Arrange
            var fpsProfile = _profileService.GetProfileById("fps");
            var platformerProfile = _profileService.GetProfileById("platformer");
            var rpgProfile = _profileService.GetProfileById("rpg");
            
            // Act
            var fpsBudgets = fpsProfile.PerformanceBudgets;
            var platformerBudgets = platformerProfile.PerformanceBudgets;
            var rpgBudgets = rpgProfile.PerformanceBudgets;
            
            // Assert
            Assert.IsTrue(fpsBudgets.MaxTriangles >= platformerBudgets.MaxTriangles, "FPS should have higher triangle budget than Platformer");
            Assert.IsTrue(fpsBudgets.MaxDrawCalls >= platformerBudgets.MaxDrawCalls, "FPS should have higher draw call budget than Platformer");
            Assert.IsTrue(fpsBudgets.MaxTextureMemoryMB >= platformerBudgets.MaxTextureMemoryMB, "FPS should have higher texture memory budget than Platformer");
            
            Assert.IsTrue(rpgBudgets.MaxAudioMemoryMB >= platformerBudgets.MaxAudioMemoryMB, "RPG should have higher audio memory budget than Platformer");
            Assert.IsTrue(rpgBudgets.MaxAIAgents >= platformerBudgets.MaxAIAgents, "RPG should have higher AI agent budget than Platformer");
        }
        
        [Test]
        public void GenreProfiles_ShouldHaveDifferentPacingConfiguration()
        {
            // Arrange
            var fpsProfile = _profileService.GetProfileById("fps");
            var platformerProfile = _profileService.GetProfileById("platformer");
            var rpgProfile = _profileService.GetProfileById("rpg");
            
            // Act
            var fpsPacing = fpsProfile.PacingConfiguration;
            var platformerPacing = platformerProfile.PacingConfiguration;
            var rpgPacing = rpgProfile.PacingConfiguration;
            
            // Assert
            Assert.IsTrue(fpsPacing.TargetBPM >= platformerPacing.TargetBPM, "FPS should have higher target BPM than Platformer");
            Assert.IsTrue(fpsPacing.TargetBPM >= rpgPacing.TargetBPM, "FPS should have higher target BPM than RPG");
            
            Assert.IsTrue(fpsPacing.TargetInteractionDensity >= platformerPacing.TargetInteractionDensity, "FPS should have higher interaction density than Platformer");
            Assert.IsTrue(fpsPacing.TargetInteractionDensity >= rpgPacing.TargetInteractionDensity, "FPS should have higher interaction density than RPG");
            
            Assert.IsTrue(fpsPacing.BreathingRoomRatio <= platformerPacing.BreathingRoomRatio, "FPS should have lower breathing room ratio than Platformer");
            Assert.IsTrue(fpsPacing.BreathingRoomRatio <= rpgPacing.BreathingRoomRatio, "FPS should have lower breathing room ratio than RPG");
        }
        
        [Test]
        public void GenreProfiles_ShouldHaveDifferentAccessibilityDefaults()
        {
            // Arrange
            var fpsProfile = _profileService.GetProfileById("fps");
            var platformerProfile = _profileService.GetProfileById("platformer");
            var rpgProfile = _profileService.GetProfileById("rpg");
            
            // Act
            var fpsAccessibility = fpsProfile.AccessibilityDefaults;
            var platformerAccessibility = platformerProfile.AccessibilityDefaults;
            var rpgAccessibility = rpgProfile.AccessibilityDefaults;
            
            // Assert
            Assert.IsFalse(fpsAccessibility.SupportsOneHandedPlay, "FPS should not support one-handed play");
            Assert.IsTrue(platformerAccessibility.SupportsOneHandedPlay, "Platformer should support one-handed play");
            Assert.IsTrue(rpgAccessibility.SupportsOneHandedPlay, "RPG should support one-handed play");
            
            Assert.IsTrue(rpgAccessibility.TextSizeMultiplier >= fpsAccessibility.TextSizeMultiplier, "RPG should have larger text size than FPS");
            Assert.IsTrue(rpgAccessibility.TextSizeMultiplier >= platformerAccessibility.TextSizeMultiplier, "RPG should have larger text size than Platformer");
        }
        
        private static GamePlan CreateTestGamePlan()
        {
            return new GamePlan
            {
                Id = "test-plan-1",
                SourceBrief = new DesignBrief
                {
                    Description = "A test game slice for validation",
                    GenreHint = "FPS",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 3,
                    Seed = 12345
                },
                Genre = "FPS",
                Description = "A test FPS game slice",
                CoreMechanics = new[] { "Shoot", "Aim", "Move", "Reload" },
                PlayerExperience = new[] { "Intense", "Fast-paced" },
                EstimatedDurationMinutes = 5,
                DifficultyProgression = new[]
                {
                    new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 2, Description = "Start" },
                    new DifficultyBeat { TimeOffsetSeconds = 60, DifficultyLevel = 3, Description = "Ramp up" }
                },
                NarrativeBeats = new[] { "Introduction", "Climax", "Resolution" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Weapon",
                        Name = "Test Weapon",
                        Description = "A test weapon",
                        IsRequired = true,
                        Priority = 5
                    }
                },
                Seed = 12345
            };
        }
    }
}
