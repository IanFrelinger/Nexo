using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Safety.Components
{
    public partial class ToxicityDetectionValidator
    {
        private readonly ILogger _logger;

        public ToxicityDetectionValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ToxicityDetectionResult> DetectToxicityAsync(string content, string context = "")
        {
            try
            {
                _logger.LogDebug("Detecting toxicity in content");

                var result = new ToxicityDetectionResult
                {
                    ToxicityScore = 0.0,
                    ToxicityTypes = new List<string>(),
                    ToxicityIndicators = new List<string>(),
                    Recommendations = new List<string>(),
                    DetectionTime = DateTime.UtcNow
                };

                // Check for profanity
                await CheckProfanityAsync(content, result);

                // Check for hate speech
                await CheckHateSpeechAsync(content, result);

                // Check for harassment
                await CheckHarassmentAsync(content, result);

                // Check for threats
                await CheckThreatsAsync(content, result);

                // Check for violence
                await CheckViolenceAsync(content, result);

                // Calculate overall toxicity score
                result.ToxicityScore = CalculateToxicityScore(result.ToxicityIndicators);

                _logger.LogDebug("Toxicity detection completed. Score: {Score}, Types: {Types}", 
                    result.ToxicityScore, string.Join(", ", result.ToxicityTypes));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toxicity detection failed");
                return new ToxicityDetectionResult
                {
                    ToxicityScore = 0.0,
                    ToxicityTypes = new List<string>(),
                    ToxicityIndicators = new List<string> { $"Detection error: {ex.Message}" },
                    Recommendations = new List<string> { "Manual toxicity review recommended" },
                    DetectionTime = DateTime.UtcNow
                };
            }
        }

        private async Task CheckProfanityAsync(string content, ToxicityDetectionResult result)
        {
            var profanityPatterns = new[]
            {
                @"\b(fuck|shit|damn|hell)\b",
                @"\b(ass|bitch|bastard)\b",
                @"\b(crap|piss|bloody)\b"
            };

            foreach (var pattern in profanityPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ToxicityTypes.Add("Profanity");
                    result.ToxicityIndicators.Add($"Profanity detected: {pattern}");
                    result.Recommendations.Add("Remove or replace profane language");
                }
            }
        }

        private async Task CheckHateSpeechAsync(string content, ToxicityDetectionResult result)
        {
            var hateSpeechPatterns = new[]
            {
                @"\b(kill|destroy|eliminate)\b.*\b(jews|muslims|blacks|gays)\b",
                @"\b(hate|despise|loathe)\b.*\b(race|religion|gender)\b",
                @"\b(inferior|superior)\b.*\b(race|ethnicity|religion)\b",
                @"\b(genocide|exterminate|purge)\b"
            };

            foreach (var pattern in hateSpeechPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ToxicityTypes.Add("Hate Speech");
                    result.ToxicityIndicators.Add($"Hate speech detected: {pattern}");
                    result.Recommendations.Add("Remove hate speech content");
                }
            }
        }

        private async Task CheckHarassmentAsync(string content, ToxicityDetectionResult result)
        {
            var harassmentPatterns = new[]
            {
                @"\b(stalk|harass|bully)\b",
                @"\b(threaten|intimidate|coerce)\b",
                @"\b(abuse|mistreat|harm)\b",
                @"\b(harassment|bullying|intimidation)\b"
            };

            foreach (var pattern in harassmentPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ToxicityTypes.Add("Harassment");
                    result.ToxicityIndicators.Add($"Harassment detected: {pattern}");
                    result.Recommendations.Add("Remove harassment content");
                }
            }
        }

        private async Task CheckThreatsAsync(string content, ToxicityDetectionResult result)
        {
            var threatPatterns = new[]
            {
                @"\b(kill|murder|assassinate)\b",
                @"\b(harm|hurt|injure)\b",
                @"\b(destroy|ruin|devastate)\b",
                @"\b(threat|warning|ultimatum)\b"
            };

            foreach (var pattern in threatPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ToxicityTypes.Add("Threats");
                    result.ToxicityIndicators.Add($"Threat detected: {pattern}");
                    result.Recommendations.Add("Remove threatening content");
                }
            }
        }

        private async Task CheckViolenceAsync(string content, ToxicityDetectionResult result)
        {
            var violencePatterns = new[]
            {
                @"\b(violence|violent|aggression)\b",
                @"\b(fight|battle|war)\b",
                @"\b(weapon|gun|knife|bomb)\b",
                @"\b(blood|gore|torture)\b"
            };

            foreach (var pattern in violencePatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ToxicityTypes.Add("Violence");
                    result.ToxicityIndicators.Add($"Violence detected: {pattern}");
                    result.Recommendations.Add("Review content for violence");
                }
            }
        }

        private double CalculateToxicityScore(List<string> toxicityIndicators)
        {
            if (toxicityIndicators.Count == 0)
                return 0.0;

            // Simple scoring based on number of toxicity indicators
            var score = Math.Min(100.0, toxicityIndicators.Count * 25.0);
            return score;
        }
    }

    public partial class ToxicityDetectionResult
    {
        public double ToxicityScore { get; set; }
        public List<string> ToxicityTypes { get; set; } = new();
        public List<string> ToxicityIndicators { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime DetectionTime { get; set; }
    }
}
