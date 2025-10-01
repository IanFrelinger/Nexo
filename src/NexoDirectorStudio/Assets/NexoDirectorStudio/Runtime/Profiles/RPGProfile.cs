using NexoDirectorStudio.DTO;
using System.Collections.Generic;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Genre profile for Role-Playing Game (RPG) games.
    /// Defines RPG-specific rules, budgets, and validation criteria.
    /// </summary>
    public sealed class RPGProfile : IGenreProfile
    {
        public string Id => "rpg";
        public string Name => "Role-Playing Game";
        public string Description => "Games focused on character development, story, and strategic gameplay";
        
        public IReadOnlyList<string> Keywords => RPGConfiguration.Keywords;
        
        public IReadOnlyList<string> CoreMechanics => RPGConfiguration.CoreMechanics;
        
        public PerformanceBudgets PerformanceBudgets => RPGConfiguration.PerformanceBudgets;
        
        public PacingConfiguration PacingConfiguration => RPGConfiguration.PacingConfiguration;
        
        public AccessibilityDefaults AccessibilityDefaults => RPGConfiguration.AccessibilityDefaults;
        
        public IReadOnlyList<string> RequiredValidators => RPGConfiguration.RequiredValidators;
        
        public IReadOnlyList<AssetRequirement> AssetRequirements => RPGAssetRequirements.AssetRequirements;
        
        public DifficultyProgressionModel DifficultyProgression => RPGConfiguration.DifficultyProgression;
        
        public int DetectGenre(DesignBrief brief)
        {
            return RPGGenreDetection.DetectGenre(brief);
        }
        
        public IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan)
        {
            return RPGValidationLogic.GetSuggestions(plan);
        }
        
        public ValidationResult ValidateGenreRequirements(GamePlan plan)
        {
            return RPGValidationLogic.ValidateGenreRequirements(plan);
        }
    }
}