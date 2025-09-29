using NUnit.Framework;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the MechanicsValidator.
    /// </summary>
    [TestFixture]
    public class Unit_Mechanics
    {
        private MechanicsValidator _validator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new MechanicsValidator();
        }
        
        [Test]
        public void ValidateAsync_WithValidFPSPlan_ShouldPass()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid FPS plan should pass validation");
            Assert.IsTrue(result.Score > 80, "Valid FPS plan should have high score");
        }
        
        [Test]
        public void ValidateAsync_WithValidPlatformerPlan_ShouldPass()
        {
            // Arrange
            var plan = CreateValidPlatformerPlan();
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid platformer plan should pass validation");
            Assert.IsTrue(result.Score > 80, "Valid platformer plan should have high score");
        }
        
        [Test]
        public void ValidateAsync_WithValidRPGPlan_ShouldPass()
        {
            // Arrange
            var plan = CreateValidRPGPlan();
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid RPG plan should pass validation");
            Assert.IsTrue(result.Score > 80, "Valid RPG plan should have high score");
        }
        
        [Test]
        public void ValidateAsync_WithFPSMissingShooting_ShouldFail()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            plan = plan with { CoreMechanics = new[] { "Move", "Run", "Crouch" } };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "FPS plan without shooting should fail validation");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Missing Shooting Mechanics"), "Should have missing shooting mechanics issue");
        }
        
        [Test]
        public void ValidateAsync_WithPlatformerMissingJumping_ShouldFail()
        {
            // Arrange
            var plan = CreateValidPlatformerPlan();
            plan = plan with { CoreMechanics = new[] { "Move", "Run", "Collect" } };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Platformer plan without jumping should fail validation");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Missing Jumping Mechanics"), "Should have missing jumping mechanics issue");
        }
        
        [Test]
        public void ValidateAsync_WithNoCoreMechanics_ShouldFail()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            plan = plan with { CoreMechanics = Array.Empty<string>() };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Plan with no core mechanics should fail validation");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "No Core Mechanics"), "Should have no core mechanics issue");
        }
        
        [Test]
        public void ValidateAsync_WithMissingBasicMovement_ShouldHaveWarning()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            plan = plan with { CoreMechanics = new[] { "Shoot", "Aim", "Reload" } };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Plan without basic movement should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Missing Basic Movement"), "Should have missing basic movement warning");
        }
        
        [Test]
        public void ValidateAsync_WithTooManyMechanics_ShouldHaveWarning()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            var manyMechanics = new[]
            {
                "Shoot", "Aim", "Reload", "Move", "Run", "Crouch", "Jump", "Climb", "Swim", "Fly", "Drive", "Ride"
            };
            plan = plan with { CoreMechanics = manyMechanics };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Plan with too many mechanics should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Too Many Core Mechanics"), "Should have too many mechanics warning");
        }
        
        [Test]
        public void ValidateAsync_WithConflictingMechanics_ShouldHaveWarning()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            plan = plan with { CoreMechanics = new[] { "Shoot", "Aim", "Jump", "Fly" } };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Plan with conflicting mechanics should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Conflicting Mechanics"), "Should have conflicting mechanics warning");
        }
        
        [Test]
        public void ValidateAsync_WithInconsistentDifficultyProgression_ShouldHaveWarning()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            var inconsistentProgression = new[]
            {
                new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 3, Description = "Start" },
                new DifficultyBeat { TimeOffsetSeconds = 30, DifficultyLevel = 2, Description = "Easy" },
                new DifficultyBeat { TimeOffsetSeconds = 60, DifficultyLevel = 4, Description = "Hard" }
            };
            plan = plan with { DifficultyProgression = inconsistentProgression };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Plan with inconsistent difficulty should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Inconsistent Difficulty Progression"), "Should have inconsistent difficulty warning");
        }
        
        [Test]
        public void ValidateAsync_WithUnknownGenre_ShouldHaveWarning()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            plan = plan with { Genre = "UnknownGenre" };
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Plan with unknown genre should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Unknown Genre"), "Should have unknown genre warning");
        }
        
        [Test]
        public void ValidateAsync_WithInvalidInput_ShouldFail()
        {
            // Arrange
            var invalidInput = "not a GamePlan";
            
            // Act
            var result = _validator.ValidateAsync(invalidInput, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Invalid input should fail validation");
            Assert.AreEqual(0, result.Score, "Invalid input should have zero score");
            Assert.IsTrue(result.Message.Contains("Invalid input type"), "Should have invalid input message");
        }
        
        [Test]
        public void ValidateAsync_ShouldGenerateSuggestions()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.Suggestions.Count > 0, "Should generate suggestions");
            Assert.IsTrue(result.Suggestions.Any(s => s.Category == "Mechanics"), "Should have mechanics suggestions");
        }
        
        [Test]
        public void ValidateAsync_ShouldHaveValidDetails()
        {
            // Arrange
            var plan = CreateValidFPSPlan();
            
            // Act
            var result = _validator.ValidateAsync(plan, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotNull(result.Details, "Details should not be null");
            Assert.IsTrue(result.Details.Contains("Genre"), "Details should contain genre");
            Assert.IsTrue(result.Details.Contains("Core Mechanics"), "Details should contain core mechanics");
        }
        
        private static GamePlan CreateValidFPSPlan()
        {
            return new GamePlan
            {
                Id = "fps-plan-1",
                SourceBrief = new DesignBrief
                {
                    Description = "A fast-paced FPS with multiple weapons",
                    GenreHint = "FPS",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 4,
                    Seed = 12345
                },
                Genre = "FPS",
                Description = "A fast-paced FPS game slice",
                CoreMechanics = new[] { "Shoot", "Aim", "Reload", "Move", "Crouch" },
                PlayerExperience = new[] { "Intense", "Fast-paced", "Challenging" },
                EstimatedDurationMinutes = 5,
                DifficultyProgression = new[]
                {
                    new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 2, Description = "Start" },
                    new DifficultyBeat { TimeOffsetSeconds = 60, DifficultyLevel = 3, Description = "Ramp up" },
                    new DifficultyBeat { TimeOffsetSeconds = 120, DifficultyLevel = 4, Description = "Peak" }
                },
                NarrativeBeats = new[] { "Introduction", "Rising Action", "Climax", "Resolution" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Weapon",
                        Name = "Assault Rifle",
                        Description = "Primary weapon",
                        IsRequired = true,
                        Priority = 5
                    },
                    new AssetRequirement
                    {
                        AssetType = "Player",
                        Name = "Player Character",
                        Description = "Player character prefab",
                        IsRequired = true,
                        Priority = 5
                    }
                },
                Seed = 12345
            };
        }
        
        private static GamePlan CreateValidPlatformerPlan()
        {
            return new GamePlan
            {
                Id = "platformer-plan-1",
                SourceBrief = new DesignBrief
                {
                    Description = "A challenging platformer with jumping mechanics",
                    GenreHint = "Platformer",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 3,
                    Seed = 12345
                },
                Genre = "Platformer",
                Description = "A challenging platformer game slice",
                CoreMechanics = new[] { "Jump", "Run", "Wall Jump", "Double Jump", "Collect" },
                PlayerExperience = new[] { "Challenging", "Rewarding", "Skill-based" },
                EstimatedDurationMinutes = 5,
                DifficultyProgression = new[]
                {
                    new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 2, Description = "Start" },
                    new DifficultyBeat { TimeOffsetSeconds = 60, DifficultyLevel = 3, Description = "Ramp up" },
                    new DifficultyBeat { TimeOffsetSeconds = 120, DifficultyLevel = 4, Description = "Peak" }
                },
                NarrativeBeats = new[] { "Introduction", "Rising Action", "Climax", "Resolution" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Platform",
                        Name = "Platform Prefab",
                        Description = "Basic platform",
                        IsRequired = true,
                        Priority = 5
                    },
                    new AssetRequirement
                    {
                        AssetType = "Player",
                        Name = "Player Character",
                        Description = "Player character prefab",
                        IsRequired = true,
                        Priority = 5
                    }
                },
                Seed = 12345
            };
        }
        
        private static GamePlan CreateValidRPGPlan()
        {
            return new GamePlan
            {
                Id = "rpg-plan-1",
                SourceBrief = new DesignBrief
                {
                    Description = "An RPG with quests and character progression",
                    GenreHint = "RPG",
                    TargetDurationMinutes = 10,
                    DifficultyLevel = 3,
                    Seed = 12345
                },
                Genre = "RPG",
                Description = "An RPG game slice with quests",
                CoreMechanics = new[] { "Talk", "Quest", "Level Up", "Inventory", "Combat" },
                PlayerExperience = new[] { "Immersive", "Story-driven", "Progressive" },
                EstimatedDurationMinutes = 10,
                DifficultyProgression = new[]
                {
                    new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 2, Description = "Start" },
                    new DifficultyBeat { TimeOffsetSeconds = 120, DifficultyLevel = 3, Description = "Ramp up" },
                    new DifficultyBeat { TimeOffsetSeconds = 240, DifficultyLevel = 4, Description = "Peak" }
                },
                NarrativeBeats = new[] { "Introduction", "Rising Action", "Climax", "Resolution" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Quest",
                        Name = "Main Quest",
                        Description = "Primary quest data",
                        IsRequired = true,
                        Priority = 5
                    },
                    new AssetRequirement
                    {
                        AssetType = "Player",
                        Name = "Player Character",
                        Description = "Player character prefab",
                        IsRequired = true,
                        Priority = 5
                    }
                },
                Seed = 12345
            };
        }
    }
}
