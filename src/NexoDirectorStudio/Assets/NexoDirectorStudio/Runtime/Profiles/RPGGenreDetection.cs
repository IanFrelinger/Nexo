using NexoDirectorStudio.DTO;
using System;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Genre detection logic for RPG games
    /// </summary>
    public static class RPGGenreDetection
    {
        public static int DetectGenre(DesignBrief brief)
        {
            if (brief == null)
                return 0;
            
            var description = brief.Description.ToLowerInvariant();
            var hint = brief.GenreHint?.ToLowerInvariant() ?? string.Empty;
            
            var score = 0;
            
            // Check for explicit genre hint
            if (hint.Contains("rpg") || hint.Contains("role-playing"))
                score += 40;
            
            // Check for RPG keywords
            var rpgKeywords = new[] { "quest", "character", "level", "experience", "skill", "inventory" };
            foreach (var keyword in rpgKeywords)
            {
                if (description.Contains(keyword))
                    score += 12;
            }
            
            // Check for story keywords
            var storyKeywords = new[] { "story", "narrative", "dialogue", "conversation", "talk" };
            foreach (var keyword in storyKeywords)
            {
                if (description.Contains(keyword))
                    score += 10;
            }
            
            // Check for adventure keywords
            var adventureKeywords = new[] { "adventure", "explore", "discover", "journey", "travel" };
            foreach (var keyword in adventureKeywords)
            {
                if (description.Contains(keyword))
                    score += 8;
            }
            
            // Check for fantasy keywords
            var fantasyKeywords = new[] { "fantasy", "magic", "medieval", "dragon", "sword", "spell" };
            foreach (var keyword in fantasyKeywords)
            {
                if (description.Contains(keyword))
                    score += 6;
            }
            
            // Check for progression keywords
            var progressionKeywords = new[] { "progress", "develop", "grow", "improve", "advance" };
            foreach (var keyword in progressionKeywords)
            {
                if (description.Contains(keyword))
                    score += 5;
            }
            
            // Check for difficulty level (RPGs can have various difficulty)
            if (brief.DifficultyLevel >= 1 && brief.DifficultyLevel <= 3)
                score += 5;
            
            // Check for longer duration
            if (brief.TargetDurationMinutes >= 5 && brief.TargetDurationMinutes <= 20)
                score += 5;
            
            return Math.Min(100, score);
        }
    }
}
