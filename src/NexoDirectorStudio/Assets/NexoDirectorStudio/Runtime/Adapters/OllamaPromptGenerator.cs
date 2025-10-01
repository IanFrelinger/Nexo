using System;
using System.Text;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Generates prompts for Ollama LLM interactions.
    /// </summary>
    public class OllamaPromptGenerator : IOllamaPromptGenerator
    {
        public string GeneratePlanningPrompt(DesignBrief brief, string genreId)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a game design expert. Generate a detailed game plan based on the following brief:");
            prompt.AppendLine();
            prompt.AppendLine($"Brief: {brief.Description}");
            prompt.AppendLine($"Genre: {genreId}");
            prompt.AppendLine($"Target Duration: {brief.TargetDurationMinutes} minutes");
            prompt.AppendLine($"Difficulty: {brief.DifficultyLevel}/5");
            prompt.AppendLine($"Seed: {brief.Seed}");
            prompt.AppendLine();
            prompt.AppendLine("Please provide a JSON response with the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"genre\": \"string\",");
            prompt.AppendLine("  \"description\": \"string\",");
            prompt.AppendLine("  \"coreMechanics\": [\"string\"],");
            prompt.AppendLine("  \"playerExperience\": [\"string\"],");
            prompt.AppendLine("  \"estimatedDurationMinutes\": number,");
            prompt.AppendLine("  \"difficultyProgression\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"beat\": \"string\",");
            prompt.AppendLine("      \"difficulty\": number,");
            prompt.AppendLine("      \"durationMinutes\": number");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"narrativeBeats\": [\"string\"],");
            prompt.AppendLine("  \"requiredAssets\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"assetType\": \"string\",");
            prompt.AppendLine("      \"name\": \"string\",");
            prompt.AppendLine("      \"description\": \"string\",");
            prompt.AppendLine("      \"isRequired\": boolean,");
            prompt.AppendLine("      \"priority\": number");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            
            return prompt.ToString();
        }

        public string GenerateAnalysisPrompt(GamePlan gamePlan)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a game design analyst. Analyze the following game plan and provide insights:");
            prompt.AppendLine();
            prompt.AppendLine($"Game Plan: {gamePlan.Description}");
            prompt.AppendLine($"Genre: {gamePlan.Genre}");
            prompt.AppendLine($"Core Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}");
            prompt.AppendLine($"Player Experience: {string.Join(", ", gamePlan.PlayerExperience)}");
            prompt.AppendLine($"Estimated Duration: {gamePlan.EstimatedDurationMinutes} minutes");
            prompt.AppendLine();
            prompt.AppendLine("Please provide a JSON response with the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"strengths\": [\"string\"],");
            prompt.AppendLine("  \"weaknesses\": [\"string\"],");
            prompt.AppendLine("  \"recommendations\": [\"string\"],");
            prompt.AppendLine("  \"riskFactors\": [\"string\"],");
            prompt.AppendLine("  \"overallScore\": number");
            prompt.AppendLine("}");
            
            return prompt.ToString();
        }

        public string GenerateFixSuggestionPrompt(ValidationReport validationReport)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a game development expert. Analyze the following validation report and suggest fixes:");
            prompt.AppendLine();
            prompt.AppendLine($"Validation Report ID: {validationReport.ReportId}");
            prompt.AppendLine($"Timestamp: {validationReport.Timestamp}");
            prompt.AppendLine();
            prompt.AppendLine("Issues:");
            foreach (var issue in validationReport.AllIssues)
            {
                prompt.AppendLine($"- {issue.Severity}: {issue.Title} - {issue.Description}");
            }
            prompt.AppendLine();
            prompt.AppendLine("Suggestions:");
            foreach (var suggestion in validationReport.AllSuggestions)
            {
                prompt.AppendLine($"- {suggestion.Priority}: {suggestion.Title} - {suggestion.Description}");
            }
            prompt.AppendLine();
            prompt.AppendLine("Please provide a JSON response with the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"suggestions\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"title\": \"string\",");
            prompt.AppendLine("      \"description\": \"string\",");
            prompt.AppendLine("      \"priority\": \"string\",");
            prompt.AppendLine("      \"category\": \"string\",");
            prompt.AppendLine("      \"estimatedEffort\": \"string\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            
            return prompt.ToString();
        }

        public string GenerateNarrativeEnhancementPrompt(GamePlan gamePlan)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a narrative design expert. Enhance the narrative elements of the following game plan:");
            prompt.AppendLine();
            prompt.AppendLine($"Game Plan: {gamePlan.Description}");
            prompt.AppendLine($"Genre: {gamePlan.Genre}");
            prompt.AppendLine($"Current Narrative Beats: {string.Join(", ", gamePlan.NarrativeBeats)}");
            prompt.AppendLine();
            prompt.AppendLine("Please provide a JSON response with enhanced narrative beats:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"narrativeBeats\": [\"string\"],");
            prompt.AppendLine("  \"storyArc\": \"string\",");
            prompt.AppendLine("  \"themes\": [\"string\"],");
            prompt.AppendLine("  \"characterMotivations\": [\"string\"]");
            prompt.AppendLine("}");
            
            return prompt.ToString();
        }
    }
}
