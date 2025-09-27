using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI response parsing functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Parses an AI-generated task analysis response and extracts the relevant analysis information.
        /// </summary>
        /// <param name="response">The raw string response from the AI model containing task analysis details.</param>
        /// <returns>An <see cref="AiTaskAnalysisResult"/> object containing the parsed analysis data such as summary, complexity assessment, and other insights.</returns>
        protected AiTaskAnalysisResult ParseTaskAnalysisResponse(string response)
        {
            try
            {
                // Simple JSON parsing - in production, use proper JSON deserialization
                var result = new AiTaskAnalysisResult();
                
                if (response.Contains("\"summary\""))
                {
                    var summaryMatch = System.Text.RegularExpressions.Regex.Match(response, "\"summary\":\\s*\"([^\"]+)\"");
                    if (summaryMatch.Success)
                        result.Summary = summaryMatch.Groups[1].Value;
                }

                if (response.Contains("\"complexityAssessment\""))
                {
                    var complexityMatch = System.Text.RegularExpressions.Regex.Match(response, "\"complexityAssessment\":\\s*\"([^\"]+)\"");
                    if (complexityMatch.Success)
                        result.ComplexityAssessment = complexityMatch.Groups[1].Value;
                }

                if (response.Contains("\"estimatedEffort\""))
                {
                    var effortMatch = System.Text.RegularExpressions.Regex.Match(response, "\"estimatedEffort\":\\s*\"([^\"]+)\"");
                    if (effortMatch.Success)
                        result.EstimatedEffort = effortMatch.Groups[1].Value;
                }

                if (response.Contains("\"recommendedApproach\""))
                {
                    var approachMatch = System.Text.RegularExpressions.Regex.Match(response, "\"recommendedApproach\":\\s*\"([^\"]+)\"");
                    if (approachMatch.Success)
                        result.RecommendedApproach = approachMatch.Groups[1].Value;
                }

                if (response.Contains("\"confidenceScore\""))
                {
                    var confidenceMatch = System.Text.RegularExpressions.Regex.Match(response, "\"confidenceScore\":\\s*([0-9.]+)");
                    if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
                        result.ConfidenceScore = confidence;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse AI task analysis response");
                return new AiTaskAnalysisResult
                {
                    Summary = "Failed to parse AI response",
                    ConfidenceScore = 0.0
                };
            }
        }

        /// <summary>
        /// Parses the response content received from the AI model to extract suggestion-related data into an <see cref="AiSuggestionsResult"/> object.
        /// </summary>
        /// <param name="response">The raw response string from the AI model containing suggestions and confidence score.</param>
        /// <returns>An <see cref="AiSuggestionsResult"/> object containing parsed improvement, code, architectural, and testing suggestions along with a confidence score.</returns>
        protected AiSuggestionsResult ParseSuggestionsResponse(string response)
        {
            try
            {
                var result = new AiSuggestionsResult();
                
                // Parse improvement suggestions
                if (response.Contains("\"improvementSuggestions\""))
                {
                    var improvementsMatch = System.Text.RegularExpressions.Regex.Match(response, "\"improvementSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (improvementsMatch.Success)
                    {
                        var suggestions = improvementsMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.ImprovementSuggestions = suggestions;
                    }
                }

                // Parse code suggestions
                if (response.Contains("\"codeSuggestions\""))
                {
                    var codeMatch = System.Text.RegularExpressions.Regex.Match(response, "\"codeSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (codeMatch.Success)
                    {
                        var suggestions = codeMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.CodeSuggestions = suggestions;
                    }
                }

                // Parse architectural suggestions
                if (response.Contains("\"architecturalSuggestions\""))
                {
                    var archMatch = System.Text.RegularExpressions.Regex.Match(response, "\"architecturalSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (archMatch.Success)
                    {
                        var suggestions = archMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.ArchitecturalSuggestions = suggestions;
                    }
                }

                // Parse testing suggestions
                if (response.Contains("\"testingSuggestions\""))
                {
                    var testMatch = System.Text.RegularExpressions.Regex.Match(response, "\"testingSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (testMatch.Success)
                    {
                        var suggestions = testMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.TestingSuggestions = suggestions;
                    }
                }

                // Parse confidence score
                if (!response.Contains("\"confidenceScore\"")) return result;
                var confidenceMatch = System.Text.RegularExpressions.Regex.Match(response, "\"confidenceScore\":\\s*([0-9.]+)");
                if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
                    result.ConfidenceScore = confidence;

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse AI suggestions response");
                return new AiSuggestionsResult
                {
                    ConfidenceScore = 0.0
                };
            }
        }
    }
}
