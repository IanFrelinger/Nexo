using System;
using System.Collections.Generic;
using System.Linq;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Utility methods for auto-fix generation.
    /// </summary>
    public static class AutoFixUtilities
    {
        /// <summary>
        /// Extracts mechanics from a validation issue.
        /// </summary>
        public static string[] ExtractMechanicsFromIssue(ValidationIssue issue)
        {
            var mechanics = new List<string>();
            
            if (issue.Description.ToLower().Contains("shooting"))
                mechanics.Add("Shooting");
            if (issue.Description.ToLower().Contains("jumping"))
                mechanics.Add("Jumping");
            if (issue.Description.ToLower().Contains("collecting"))
                mechanics.Add("Collecting");
            if (issue.Description.ToLower().Contains("dialogue"))
                mechanics.Add("Dialogue");
            if (issue.Description.ToLower().Contains("inventory"))
                mechanics.Add("Inventory");
            if (issue.Description.ToLower().Contains("movement"))
                mechanics.Add("Movement");
            if (issue.Description.ToLower().Contains("interaction"))
                mechanics.Add("Interaction");
            
            return mechanics.ToArray();
        }

        /// <summary>
        /// Creates an asset requirement from a validation issue.
        /// </summary>
        public static AssetRequirement CreateAssetFromIssue(ValidationIssue issue)
        {
            var assetType = ExtractAssetTypeFromIssue(issue);
            var assetName = ExtractAssetNameFromIssue(issue);
            
            return new AssetRequirement(
                assetName,
                assetType,
                $"Generated from {issue.Category} issue",
                true,
                1
            );
        }

        /// <summary>
        /// Extracts asset type from a validation issue.
        /// </summary>
        public static string ExtractAssetTypeFromIssue(ValidationIssue issue)
        {
            if (issue.Category.ToLower().Contains("weapon"))
                return "Weapon";
            if (issue.Category.ToLower().Contains("enemy"))
                return "Enemy";
            if (issue.Category.ToLower().Contains("platform"))
                return "Platform";
            if (issue.Category.ToLower().Contains("collectible"))
                return "Collectible";
            if (issue.Category.ToLower().Contains("npc"))
                return "NPC";
            if (issue.Category.ToLower().Contains("ui"))
                return "UI";
            if (issue.Category.ToLower().Contains("audio"))
                return "Audio";
            if (issue.Category.ToLower().Contains("texture"))
                return "Texture";
            
            return "Component";
        }

        /// <summary>
        /// Extracts asset name from a validation issue.
        /// </summary>
        public static string ExtractAssetNameFromIssue(ValidationIssue issue)
        {
            return issue.Title.Replace(" ", "").Replace("-", "");
        }

        /// <summary>
        /// Calculates optimal difficulty based on validation issue.
        /// </summary>
        public static int CalculateOptimalDifficulty(ValidationIssue issue)
        {
            return issue.Severity switch
            {
                ValidationSeverity.Critical => 5,
                ValidationSeverity.Error => 4,
                ValidationSeverity.Warning => 3,
                ValidationSeverity.Info => 2,
                _ => 3
            };
        }

        /// <summary>
        /// Calculates optimal duration based on validation issue.
        /// </summary>
        public static int CalculateOptimalDuration(ValidationIssue issue)
        {
            return issue.Severity switch
            {
                ValidationSeverity.Critical => 10,
                ValidationSeverity.Error => 8,
                ValidationSeverity.Warning => 5,
                ValidationSeverity.Info => 3,
                _ => 5
            };
        }

        /// <summary>
        /// Extracts mechanics from a validation suggestion.
        /// </summary>
        public static string[] ExtractMechanicsFromSuggestion(ValidationSuggestion suggestion)
        {
            var mechanics = new List<string>();
            
            if (suggestion.Description.ToLower().Contains("shooting"))
                mechanics.Add("Shooting");
            if (suggestion.Description.ToLower().Contains("jumping"))
                mechanics.Add("Jumping");
            if (suggestion.Description.ToLower().Contains("collecting"))
                mechanics.Add("Collecting");
            if (suggestion.Description.ToLower().Contains("dialogue"))
                mechanics.Add("Dialogue");
            if (suggestion.Description.ToLower().Contains("inventory"))
                mechanics.Add("Inventory");
            if (suggestion.Description.ToLower().Contains("movement"))
                mechanics.Add("Movement");
            if (suggestion.Description.ToLower().Contains("interaction"))
                mechanics.Add("Interaction");
            
            return mechanics.ToArray();
        }

        /// <summary>
        /// Creates an asset requirement from a validation suggestion.
        /// </summary>
        public static AssetRequirement CreateAssetFromSuggestion(ValidationSuggestion suggestion)
        {
            var assetType = ExtractAssetTypeFromSuggestion(suggestion);
            var assetName = suggestion.Title.Replace(" ", "").Replace("-", "");
            
            return new AssetRequirement(
                assetName,
                assetType,
                $"Generated from {suggestion.Category} suggestion",
                true,
                1
            );
        }

        /// <summary>
        /// Extracts asset type from a validation suggestion.
        /// </summary>
        public static string ExtractAssetTypeFromSuggestion(ValidationSuggestion suggestion)
        {
            if (suggestion.Category.ToLower().Contains("weapon"))
                return "Weapon";
            if (suggestion.Category.ToLower().Contains("enemy"))
                return "Enemy";
            if (suggestion.Category.ToLower().Contains("platform"))
                return "Platform";
            if (suggestion.Category.ToLower().Contains("collectible"))
                return "Collectible";
            if (suggestion.Category.ToLower().Contains("npc"))
                return "NPC";
            if (suggestion.Category.ToLower().Contains("ui"))
                return "UI";
            if (suggestion.Category.ToLower().Contains("audio"))
                return "Audio";
            if (suggestion.Category.ToLower().Contains("texture"))
                return "Texture";
            
            return "Component";
        }

        /// <summary>
        /// Extracts narrative beats from a validation suggestion.
        /// </summary>
        public static string[] ExtractNarrativeBeatsFromSuggestion(ValidationSuggestion suggestion)
        {
            var beats = new List<string>();
            
            if (suggestion.Description.ToLower().Contains("introduction"))
                beats.Add("Introduction");
            if (suggestion.Description.ToLower().Contains("conflict"))
                beats.Add("Conflict");
            if (suggestion.Description.ToLower().Contains("resolution"))
                beats.Add("Resolution");
            if (suggestion.Description.ToLower().Contains("climax"))
                beats.Add("Climax");
            if (suggestion.Description.ToLower().Contains("ending"))
                beats.Add("Ending");
            
            return beats.ToArray();
        }

        /// <summary>
        /// Extracts player experience elements from a validation suggestion.
        /// </summary>
        public static string[] ExtractPlayerExperienceFromSuggestion(ValidationSuggestion suggestion)
        {
            var experiences = new List<string>();
            
            if (suggestion.Description.ToLower().Contains("challenge"))
                experiences.Add("Challenge");
            if (suggestion.Description.ToLower().Contains("reward"))
                experiences.Add("Reward");
            if (suggestion.Description.ToLower().Contains("exploration"))
                experiences.Add("Exploration");
            if (suggestion.Description.ToLower().Contains("discovery"))
                experiences.Add("Discovery");
            if (suggestion.Description.ToLower().Contains("achievement"))
                experiences.Add("Achievement");
            
            return experiences.ToArray();
        }

        /// <summary>
        /// Extracts accessibility elements from a validation suggestion.
        /// </summary>
        public static string[] ExtractAccessibilityFromSuggestion(ValidationSuggestion suggestion)
        {
            var accessibility = new List<string>();
            
            if (suggestion.Description.ToLower().Contains("color"))
                accessibility.Add("Color Contrast");
            if (suggestion.Description.ToLower().Contains("text"))
                accessibility.Add("Text Readability");
            if (suggestion.Description.ToLower().Contains("audio"))
                accessibility.Add("Audio Accessibility");
            if (suggestion.Description.ToLower().Contains("motion"))
                accessibility.Add("Motion Accessibility");
            if (suggestion.Description.ToLower().Contains("keyboard"))
                accessibility.Add("Keyboard Navigation");
            
            return accessibility.ToArray();
        }

        /// <summary>
        /// Generates a summary for auto-fix proposals.
        /// </summary>
        public static string GenerateSummary(IReadOnlyList<AutoFix> fixes)
        {
            if (!fixes.Any())
                return "No fixes available";

            var automaticCount = fixes.Count(f => f.CanApplyAutomatically);
            var manualCount = fixes.Count - automaticCount;
            
            return $"{fixes.Count} fixes proposed ({automaticCount} automatic, {manualCount} manual)";
        }

        /// <summary>
        /// Generates a description for auto-fix proposals.
        /// </summary>
        public static string GenerateDescription(IReadOnlyList<AutoFix> fixes)
        {
            if (!fixes.Any())
                return "No fixes available";

            var categories = fixes.GroupBy(f => f.Category).Select(g => $"{g.Key} ({g.Count()})");
            return $"Fixes for: {string.Join(", ", categories)}";
        }

        /// <summary>
        /// Calculates estimated effort for auto-fix proposals.
        /// </summary>
        public static string CalculateEstimatedEffort(IReadOnlyList<AutoFix> fixes)
        {
            if (!fixes.Any())
                return "None";

            var effortCounts = fixes.GroupBy(f => f.EstimatedEffort)
                .ToDictionary(g => g.Key, g => g.Count());

            var maxEffort = effortCounts.Keys.Max();
            var maxCount = effortCounts[maxEffort];

            if (maxCount == fixes.Count)
                return maxEffort;

            return $"{maxEffort} (mostly)";
        }
    }
}
