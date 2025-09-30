using NUnit.Framework;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests for genre profile budgets and pacing configuration.
    /// </summary>
    [TestFixture]
    public class Unit_ProfileBudgets
    {
        private FPSProfile _fpsProfile;
        private PlatformerProfile _platformerProfile;
        private RPGProfile _rpgProfile;
        
        [SetUp]
        public void SetUp()
        {
            _fpsProfile = new FPSProfile();
            _platformerProfile = new PlatformerProfile();
            _rpgProfile = new RPGProfile();
        }
        
        [Test]
        public void FPSProfile_ShouldHaveHighPerformanceBudgets()
        {
            // Act
            var budgets = _fpsProfile.PerformanceBudgets;
            
            // Assert
            Assert.IsTrue(budgets.MaxTriangles >= 100000, "FPS should have high triangle budget");
            Assert.IsTrue(budgets.MaxDrawCalls >= 100, "FPS should have high draw call budget");
            Assert.IsTrue(budgets.MaxTextureMemoryMB >= 500, "FPS should have high texture memory budget");
            Assert.IsTrue(budgets.MaxAudioMemoryMB >= 200, "FPS should have high audio memory budget");
            Assert.IsTrue(budgets.MaxActiveLights >= 8, "FPS should have high light budget");
            Assert.IsTrue(budgets.MaxParticles >= 1000, "FPS should have high particle budget");
            Assert.IsTrue(budgets.MaxPhysicsObjects >= 30, "FPS should have moderate physics budget");
            Assert.IsTrue(budgets.MaxAIAgents >= 20, "FPS should have high AI agent budget");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveModeratePerformanceBudgets()
        {
            // Act
            var budgets = _platformerProfile.PerformanceBudgets;
            
            // Assert
            Assert.IsTrue(budgets.MaxTriangles >= 50000, "Platformer should have moderate triangle budget");
            Assert.IsTrue(budgets.MaxDrawCalls >= 50, "Platformer should have moderate draw call budget");
            Assert.IsTrue(budgets.MaxTextureMemoryMB >= 200, "Platformer should have moderate texture memory budget");
            Assert.IsTrue(budgets.MaxAudioMemoryMB >= 100, "Platformer should have moderate audio memory budget");
            Assert.IsTrue(budgets.MaxActiveLights >= 4, "Platformer should have moderate light budget");
            Assert.IsTrue(budgets.MaxParticles >= 200, "Platformer should have moderate particle budget");
            Assert.IsTrue(budgets.MaxPhysicsObjects >= 100, "Platformer should have high physics budget");
            Assert.IsTrue(budgets.MaxAIAgents >= 5, "Platformer should have moderate AI agent budget");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveModeratePerformanceBudgets()
        {
            // Act
            var budgets = _rpgProfile.PerformanceBudgets;
            
            // Assert
            Assert.IsTrue(budgets.MaxTriangles >= 80000, "RPG should have moderate triangle budget");
            Assert.IsTrue(budgets.MaxDrawCalls >= 80, "RPG should have moderate draw call budget");
            Assert.IsTrue(budgets.MaxTextureMemoryMB >= 400, "RPG should have moderate texture memory budget");
            Assert.IsTrue(budgets.MaxAudioMemoryMB >= 300, "RPG should have high audio memory budget");
            Assert.IsTrue(budgets.MaxActiveLights >= 6, "RPG should have moderate light budget");
            Assert.IsTrue(budgets.MaxParticles >= 500, "RPG should have moderate particle budget");
            Assert.IsTrue(budgets.MaxPhysicsObjects >= 100, "RPG should have moderate physics budget");
            Assert.IsTrue(budgets.MaxAIAgents >= 15, "RPG should have moderate AI agent budget");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveHighIntensityPacing()
        {
            // Act
            var pacing = _fpsProfile.PacingConfiguration;
            
            // Assert
            Assert.IsTrue(pacing.TargetBPM >= 120, "FPS should have high target BPM");
            Assert.IsTrue(pacing.MinEventInterval <= 10, "FPS should have short minimum event interval");
            Assert.IsTrue(pacing.MaxEventInterval <= 20, "FPS should have short maximum event interval");
            Assert.IsTrue(pacing.TargetInteractionDensity >= 10, "FPS should have high interaction density");
            Assert.IsTrue(pacing.BreathingRoomRatio <= 0.3f, "FPS should have low breathing room ratio");
            Assert.IsTrue(pacing.DifficultyRampRate >= 0.1f, "FPS should have high difficulty ramp rate");
            Assert.AreEqual("exponential", pacing.PacingCurveType, "FPS should have exponential pacing curve");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveModeratePacing()
        {
            // Act
            var pacing = _platformerProfile.PacingConfiguration;
            
            // Assert
            Assert.IsTrue(pacing.TargetBPM >= 100, "Platformer should have moderate target BPM");
            Assert.IsTrue(pacing.MinEventInterval >= 5, "Platformer should have moderate minimum event interval");
            Assert.IsTrue(pacing.MaxEventInterval >= 15, "Platformer should have moderate maximum event interval");
            Assert.IsTrue(pacing.TargetInteractionDensity >= 5, "Platformer should have moderate interaction density");
            Assert.IsTrue(pacing.BreathingRoomRatio >= 0.3f, "Platformer should have moderate breathing room ratio");
            Assert.IsTrue(pacing.DifficultyRampRate >= 0.05f, "Platformer should have moderate difficulty ramp rate");
            Assert.AreEqual("linear", pacing.PacingCurveType, "Platformer should have linear pacing curve");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveLowIntensityPacing()
        {
            // Act
            var pacing = _rpgProfile.PacingConfiguration;
            
            // Assert
            Assert.IsTrue(pacing.TargetBPM <= 120, "RPG should have low target BPM");
            Assert.IsTrue(pacing.MinEventInterval >= 10, "RPG should have long minimum event interval");
            Assert.IsTrue(pacing.MaxEventInterval >= 30, "RPG should have long maximum event interval");
            Assert.IsTrue(pacing.TargetInteractionDensity <= 8, "RPG should have low interaction density");
            Assert.IsTrue(pacing.BreathingRoomRatio >= 0.5f, "RPG should have high breathing room ratio");
            Assert.IsTrue(pacing.DifficultyRampRate <= 0.1f, "RPG should have low difficulty ramp rate");
            Assert.AreEqual("logarithmic", pacing.PacingCurveType, "RPG should have logarithmic pacing curve");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveStandardAccessibilityDefaults()
        {
            // Act
            var accessibility = _fpsProfile.AccessibilityDefaults;
            
            // Assert
            Assert.IsTrue(accessibility.ColorContrastRatio >= 4.5f, "FPS should have standard contrast ratio");
            Assert.IsTrue(accessibility.TextSizeMultiplier >= 1.0f, "FPS should have standard text size");
            Assert.IsTrue(accessibility.CanReduceMotion, "FPS should support motion reduction");
            Assert.IsTrue(accessibility.CanSubstituteAudio, "FPS should support audio substitution");
            Assert.IsTrue(accessibility.CanSubstituteColor, "FPS should support color substitution");
            Assert.IsTrue(accessibility.DifficultyOptions.Count >= 3, "FPS should have multiple difficulty options");
            Assert.IsFalse(accessibility.SupportsOneHandedPlay, "FPS should not support one-handed play");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveAccessibleDefaults()
        {
            // Act
            var accessibility = _platformerProfile.AccessibilityDefaults;
            
            // Assert
            Assert.IsTrue(accessibility.ColorContrastRatio >= 4.5f, "Platformer should have standard contrast ratio");
            Assert.IsTrue(accessibility.TextSizeMultiplier >= 1.0f, "Platformer should have readable text size");
            Assert.IsTrue(accessibility.CanReduceMotion, "Platformer should support motion reduction");
            Assert.IsTrue(accessibility.CanSubstituteAudio, "Platformer should support audio substitution");
            Assert.IsTrue(accessibility.CanSubstituteColor, "Platformer should support color substitution");
            Assert.IsTrue(accessibility.DifficultyOptions.Count >= 3, "Platformer should have multiple difficulty options");
            Assert.IsTrue(accessibility.SupportsOneHandedPlay, "Platformer should support one-handed play");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveAccessibleDefaults()
        {
            // Act
            var accessibility = _rpgProfile.AccessibilityDefaults;
            
            // Assert
            Assert.IsTrue(accessibility.ColorContrastRatio >= 4.5f, "RPG should have standard contrast ratio");
            Assert.IsTrue(accessibility.TextSizeMultiplier >= 1.0f, "RPG should have readable text size");
            Assert.IsTrue(accessibility.CanReduceMotion, "RPG should support motion reduction");
            Assert.IsTrue(accessibility.CanSubstituteAudio, "RPG should support audio substitution");
            Assert.IsTrue(accessibility.CanSubstituteColor, "RPG should support color substitution");
            Assert.IsTrue(accessibility.DifficultyOptions.Count >= 3, "RPG should have multiple difficulty options");
            Assert.IsTrue(accessibility.SupportsOneHandedPlay, "RPG should support one-handed play");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveSteepDifficultyProgression()
        {
            // Act
            var progression = _fpsProfile.DifficultyProgression;
            
            // Assert
            Assert.IsTrue(progression.StartingDifficulty >= 1, "FPS should have reasonable starting difficulty");
            Assert.IsTrue(progression.PeakDifficulty >= 4, "FPS should have high peak difficulty");
            Assert.IsTrue(progression.TimeToPeakMinutes <= 5, "FPS should have quick ramp-up");
            Assert.AreEqual("exponential", progression.DifficultyCurveType, "FPS should have exponential difficulty curve");
            Assert.IsFalse(progression.AllowDifficultyDecrease, "FPS should not allow difficulty decrease");
            Assert.IsTrue(progression.MinDifficultyChangeInterval <= 30, "FPS should allow quick difficulty changes");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveGradualDifficultyProgression()
        {
            // Act
            var progression = _platformerProfile.DifficultyProgression;
            
            // Assert
            Assert.IsTrue(progression.StartingDifficulty >= 1, "Platformer should have reasonable starting difficulty");
            Assert.IsTrue(progression.PeakDifficulty >= 3, "Platformer should have moderate peak difficulty");
            Assert.IsTrue(progression.TimeToPeakMinutes >= 5, "Platformer should have gradual ramp-up");
            Assert.AreEqual("linear", progression.DifficultyCurveType, "Platformer should have linear difficulty curve");
            Assert.IsTrue(progression.AllowDifficultyDecrease, "Platformer should allow difficulty decrease");
            Assert.IsTrue(progression.MinDifficultyChangeInterval >= 30, "Platformer should have moderate difficulty change interval");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveSlowDifficultyProgression()
        {
            // Act
            var progression = _rpgProfile.DifficultyProgression;
            
            // Assert
            Assert.IsTrue(progression.StartingDifficulty >= 1, "RPG should have reasonable starting difficulty");
            Assert.IsTrue(progression.PeakDifficulty >= 3, "RPG should have moderate peak difficulty");
            Assert.IsTrue(progression.TimeToPeakMinutes >= 10, "RPG should have slow ramp-up");
            Assert.AreEqual("logarithmic", progression.DifficultyCurveType, "RPG should have logarithmic difficulty curve");
            Assert.IsTrue(progression.AllowDifficultyDecrease, "RPG should allow difficulty decrease");
            Assert.IsTrue(progression.MinDifficultyChangeInterval >= 60, "RPG should have slow difficulty change interval");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveRequiredValidators()
        {
            // Act
            var validators = _fpsProfile.RequiredValidators;
            
            // Assert
            Assert.IsTrue(validators.Count > 0, "FPS should have required validators");
            Assert.IsTrue(validators.Contains("PlayabilityValidator"), "FPS should require playability validator");
            Assert.IsTrue(validators.Contains("MechanicsValidator"), "FPS should require mechanics validator");
            Assert.IsTrue(validators.Contains("PerformanceValidator"), "FPS should require performance validator");
            Assert.IsTrue(validators.Contains("PacingValidator"), "FPS should require pacing validator");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveRequiredValidators()
        {
            // Act
            var validators = _platformerProfile.RequiredValidators;
            
            // Assert
            Assert.IsTrue(validators.Count > 0, "Platformer should have required validators");
            Assert.IsTrue(validators.Contains("PlayabilityValidator"), "Platformer should require playability validator");
            Assert.IsTrue(validators.Contains("MechanicsValidator"), "Platformer should require mechanics validator");
            Assert.IsTrue(validators.Contains("PerformanceValidator"), "Platformer should require performance validator");
            Assert.IsTrue(validators.Contains("PacingValidator"), "Platformer should require pacing validator");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveRequiredValidators()
        {
            // Act
            var validators = _rpgProfile.RequiredValidators;
            
            // Assert
            Assert.IsTrue(validators.Count > 0, "RPG should have required validators");
            Assert.IsTrue(validators.Contains("PlayabilityValidator"), "RPG should require playability validator");
            Assert.IsTrue(validators.Contains("MechanicsValidator"), "RPG should require mechanics validator");
            Assert.IsTrue(validators.Contains("PerformanceValidator"), "RPG should require performance validator");
            Assert.IsTrue(validators.Contains("PacingValidator"), "RPG should require pacing validator");
        }
        
        [Test]
        public void FPSProfile_ShouldHaveAssetRequirements()
        {
            // Act
            var assets = _fpsProfile.AssetRequirements;
            
            // Assert
            Assert.IsTrue(assets.Count > 0, "FPS should have asset requirements");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Weapon"), "FPS should require weapon assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Enemy"), "FPS should require enemy assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Environment"), "FPS should require environment assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Audio"), "FPS should require audio assets");
        }
        
        [Test]
        public void PlatformerProfile_ShouldHaveAssetRequirements()
        {
            // Act
            var assets = _platformerProfile.AssetRequirements;
            
            // Assert
            Assert.IsTrue(assets.Count > 0, "Platformer should have asset requirements");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Platform"), "Platformer should require platform assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Collectible"), "Platformer should require collectible assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Hazard"), "Platformer should require hazard assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Powerup"), "Platformer should require powerup assets");
        }
        
        [Test]
        public void RPGProfile_ShouldHaveAssetRequirements()
        {
            // Act
            var assets = _rpgProfile.AssetRequirements;
            
            // Assert
            Assert.IsTrue(assets.Count > 0, "RPG should have asset requirements");
            Assert.IsTrue(assets.Any(a => a.AssetType == "NPC"), "RPG should require NPC assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Quest"), "RPG should require quest assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Inventory"), "RPG should require inventory assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Character"), "RPG should require character assets");
            Assert.IsTrue(assets.Any(a => a.AssetType == "Dialogue"), "RPG should require dialogue assets");
        }
        
        [Test]
        public void PerformanceBudgets_ShouldHaveReasonableValues()
        {
            // Act
            var fpsBudgets = _fpsProfile.PerformanceBudgets;
            var platformerBudgets = _platformerProfile.PerformanceBudgets;
            var rpgBudgets = _rpgProfile.PerformanceBudgets;
            
            // Assert
            Assert.IsTrue(fpsBudgets.MaxTriangles > 0, "FPS triangle budget should be positive");
            Assert.IsTrue(platformerBudgets.MaxTriangles > 0, "Platformer triangle budget should be positive");
            Assert.IsTrue(rpgBudgets.MaxTriangles > 0, "RPG triangle budget should be positive");
            
            Assert.IsTrue(fpsBudgets.MaxDrawCalls > 0, "FPS draw call budget should be positive");
            Assert.IsTrue(platformerBudgets.MaxDrawCalls > 0, "Platformer draw call budget should be positive");
            Assert.IsTrue(rpgBudgets.MaxDrawCalls > 0, "RPG draw call budget should be positive");
            
            Assert.IsTrue(fpsBudgets.MaxTextureMemoryMB > 0, "FPS texture memory budget should be positive");
            Assert.IsTrue(platformerBudgets.MaxTextureMemoryMB > 0, "Platformer texture memory budget should be positive");
            Assert.IsTrue(rpgBudgets.MaxTextureMemoryMB > 0, "RPG texture memory budget should be positive");
        }
        
        [Test]
        public void PacingConfiguration_ShouldHaveReasonableValues()
        {
            // Act
            var fpsPacing = _fpsProfile.PacingConfiguration;
            var platformerPacing = _platformerProfile.PacingConfiguration;
            var rpgPacing = _rpgProfile.PacingConfiguration;
            
            // Assert
            Assert.IsTrue(fpsPacing.TargetBPM > 0, "FPS target BPM should be positive");
            Assert.IsTrue(platformerPacing.TargetBPM > 0, "Platformer target BPM should be positive");
            Assert.IsTrue(rpgPacing.TargetBPM > 0, "RPG target BPM should be positive");
            
            Assert.IsTrue(fpsPacing.MinEventInterval > 0, "FPS min event interval should be positive");
            Assert.IsTrue(platformerPacing.MinEventInterval > 0, "Platformer min event interval should be positive");
            Assert.IsTrue(rpgPacing.MinEventInterval > 0, "RPG min event interval should be positive");
            
            Assert.IsTrue(fpsPacing.MaxEventInterval > fpsPacing.MinEventInterval, "FPS max event interval should be greater than min");
            Assert.IsTrue(platformerPacing.MaxEventInterval > platformerPacing.MinEventInterval, "Platformer max event interval should be greater than min");
            Assert.IsTrue(rpgPacing.MaxEventInterval > rpgPacing.MinEventInterval, "RPG max event interval should be greater than min");
        }
    }
}
