using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Parses responses from Ollama LLM interactions.
    /// </summary>
    public class OllamaResponseParser : IOllamaResponseParser
    {
        public GamePlan ParseGamePlanResponse(string response, DesignBrief brief, int seed)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var genre = root.GetProperty("genre").GetString() ?? brief.GenreHint;
                var description = root.GetProperty("description").GetString() ?? brief.Description;
                var coreMechanics = ParseStringArray(root, "coreMechanics");
                var playerExperience = ParseStringArray(root, "playerExperience");
                var estimatedDuration = root.GetProperty("estimatedDurationMinutes").GetInt32();
                var difficultyProgression = ParseDifficultyProgression(root);
                var narrativeBeats = ParseStringArray(root, "narrativeBeats");
                var requiredAssets = ParseAssets(root.GetProperty("requiredAssets"));

                return new GamePlan(
                    Id: Guid.NewGuid().ToString(),
                    SourceBrief: brief,
                    Genre: genre,
                    Description: description,
                    CoreMechanics: coreMechanics,
                    PlayerExperience: playerExperience,
                    EstimatedDurationMinutes: estimatedDuration,
                    DifficultyProgression: difficultyProgression,
                    NarrativeBeats: narrativeBeats,
                    RequiredAssets: requiredAssets,
                    Seed: seed,
                    GeneratedAt: DateTimeOffset.UtcNow,
                    Hash: response.GetHashCode().ToString()
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse game plan response: {ex.Message}", ex);
            }
        }

        public GamePlanAnalysis ParseAnalysisResponse(string response)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var strengths = ParseStringArray(root, "strengths");
                var weaknesses = ParseStringArray(root, "weaknesses");
                var recommendations = ParseStringArray(root, "recommendations");
                var riskFactors = ParseStringArray(root, "riskFactors");
                var overallScore = root.GetProperty("overallScore").GetSingle();

                return new GamePlanAnalysis
                {
                    Strengths = strengths,
                    Weaknesses = weaknesses,
                    Recommendations = recommendations,
                    RiskFactors = riskFactors,
                    OverallScore = overallScore,
                    AnalyzedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse analysis response: {ex.Message}", ex);
            }
        }

        public AutoFixSuggestions ParseFixSuggestionResponse(string response)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var suggestions = new List<AutoFixSuggestion>();
                var suggestionsArray = root.GetProperty("suggestions").EnumerateArray();

                foreach (var suggestionElement in suggestionsArray)
                {
                    var suggestion = new AutoFixSuggestion
                    {
                        Title = suggestionElement.GetProperty("title").GetString() ?? "",
                        Description = suggestionElement.GetProperty("description").GetString() ?? "",
                        Priority = Enum.TryParse<ValidationPriority>(suggestionElement.GetProperty("priority").GetString(), out var priority) 
                            ? priority : ValidationPriority.Medium,
                        Category = suggestionElement.GetProperty("category").GetString() ?? "",
                        EstimatedEffort = suggestionElement.GetProperty("estimatedEffort").GetString() ?? "Unknown"
                    };
                    suggestions.Add(suggestion);
                }

                return new AutoFixSuggestions
                {
                    Suggestions = suggestions,
                    GeneratedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse fix suggestion response: {ex.Message}", ex);
            }
        }

        public GamePlan ParseNarrativeEnhancementResponse(string response, GamePlan originalPlan)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var enhancedNarrativeBeats = ParseStringArray(root, "narrativeBeats");
                var storyArc = root.GetProperty("storyArc").GetString() ?? "";
                var themes = ParseStringArray(root, "themes");
                var characterMotivations = ParseStringArray(root, "characterMotivations");

                // Create enhanced game plan with updated narrative elements
                return new GamePlan(
                    Id: originalPlan.Id,
                    SourceBrief: originalPlan.SourceBrief,
                    Genre: originalPlan.Genre,
                    Description: originalPlan.Description,
                    CoreMechanics: originalPlan.CoreMechanics,
                    PlayerExperience: originalPlan.PlayerExperience,
                    EstimatedDurationMinutes: originalPlan.EstimatedDurationMinutes,
                    DifficultyProgression: originalPlan.DifficultyProgression,
                    NarrativeBeats: enhancedNarrativeBeats,
                    RequiredAssets: originalPlan.RequiredAssets,
                    Seed: originalPlan.Seed,
                    GeneratedAt: originalPlan.GeneratedAt,
                    Hash: originalPlan.Hash
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse narrative enhancement response: {ex.Message}", ex);
            }
        }

        private static List<string> ParseStringArray(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var arrayElement))
            {
                return arrayElement.EnumerateArray()
                    .Select(item => item.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            return new List<string>();
        }

        private static List<DifficultyBeat> ParseDifficultyProgression(JsonElement root)
        {
            var progression = new List<DifficultyBeat>();
            
            if (root.TryGetProperty("difficultyProgression", out var progressionElement))
            {
                foreach (var beatElement in progressionElement.EnumerateArray())
                {
                    var beat = new DifficultyBeat(
                        Beat: beatElement.GetProperty("beat").GetString() ?? "",
                        Difficulty: beatElement.GetProperty("difficulty").GetInt32(),
                        DurationMinutes: beatElement.GetProperty("durationMinutes").GetInt32()
                    );
                    progression.Add(beat);
                }
            }
            
            return progression;
        }

        private static List<AssetRequirement> ParseAssets(JsonElement assetsElement)
        {
            var assets = new List<AssetRequirement>();
            
            foreach (var assetElement in assetsElement.EnumerateArray())
            {
                var asset = new AssetRequirement(
                    AssetType: assetElement.GetProperty("assetType").GetString() ?? "",
                    Name: assetElement.GetProperty("name").GetString() ?? "",
                    Description: assetElement.GetProperty("description").GetString() ?? "",
                    IsRequired: assetElement.GetProperty("isRequired").GetBoolean(),
                    Priority: assetElement.GetProperty("priority").GetInt32()
                );
                assets.Add(asset);
            }
            
            return assets;
        }
    }
}
