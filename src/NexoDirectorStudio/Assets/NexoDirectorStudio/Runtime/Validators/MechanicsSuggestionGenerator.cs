using NexoDirectorStudio.DTO;
using System.Collections.Generic;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Generates mechanics suggestions for game plans
    /// </summary>
    public static class MechanicsSuggestionGenerator
    {
        public static IReadOnlyList<ValidationSuggestion> GenerateMechanicsSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Suggest adding genre-specific mechanics
            switch (plan.Genre)
            {
                case "FPS":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Reload Mechanics",
                        Description = "Consider adding reload mechanics to enhance the FPS experience.",
                        Priority = 3,
                        Effort = "Low"
                    });
                    break;
                case "Platformer":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Wall Jumping",
                        Description = "Consider adding wall jumping mechanics to increase platforming options.",
                        Priority = 4,
                        Effort = "Medium"
                    });
                    break;
                case "RPG":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Skill Trees",
                        Description = "Consider adding skill trees to provide character customization.",
                        Priority = 3,
                        Effort = "High"
                    });
                    break;
            }
            
            // Suggest adding accessibility mechanics
            suggestions.Add(new ValidationSuggestion
            {
                Category = "Mechanics",
                Title = "Add Accessibility Options",
                Description = "Consider adding accessibility options like colorblind support or difficulty toggles.",
                Priority = 4,
                Effort = "Medium"
            });
            
            return suggestions;
        }
    }
}
