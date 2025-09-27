using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Safety.Components
{
    public partial class BiasDetectionValidator
    {
        private readonly ILogger _logger;

        public BiasDetectionValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BiasDetectionResult> DetectBiasAsync(string content, string context = "")
        {
            try
            {
                _logger.LogDebug("Detecting bias in content");

                var result = new BiasDetectionResult
                {
                    BiasScore = 0.0,
                    BiasTypes = new List<string>(),
                    BiasIndicators = new List<string>(),
                    Recommendations = new List<string>(),
                    DetectionTime = DateTime.UtcNow
                };

                // Check for gender bias
                await CheckGenderBiasAsync(content, result);

                // Check for racial bias
                await CheckRacialBiasAsync(content, result);

                // Check for age bias
                await CheckAgeBiasAsync(content, result);

                // Check for cultural bias
                await CheckCulturalBiasAsync(content, result);

                // Check for socioeconomic bias
                await CheckSocioeconomicBiasAsync(content, result);

                // Calculate overall bias score
                result.BiasScore = CalculateBiasScore(result.BiasIndicators);

                _logger.LogDebug("Bias detection completed. Score: {Score}, Types: {Types}", 
                    result.BiasScore, string.Join(", ", result.BiasTypes));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bias detection failed");
                return new BiasDetectionResult
                {
                    BiasScore = 0.0,
                    BiasTypes = new List<string>(),
                    BiasIndicators = new List<string> { $"Detection error: {ex.Message}" },
                    Recommendations = new List<string> { "Manual bias review recommended" },
                    DetectionTime = DateTime.UtcNow
                };
            }
        }

        private async Task CheckGenderBiasAsync(string content, BiasDetectionResult result)
        {
            var genderBiasPatterns = new[]
            {
                @"\b(he|him|his)\b.*\b(engineer|programmer|scientist)\b",
                @"\b(she|her|hers)\b.*\b(nurse|teacher|secretary)\b",
                @"\b(men|man)\b.*\b(strong|tough|aggressive)\b",
                @"\b(women|woman)\b.*\b(emotional|sensitive|weak)\b"
            };

            foreach (var pattern in genderBiasPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.BiasTypes.Add("Gender Bias");
                    result.BiasIndicators.Add($"Gender bias pattern detected: {pattern}");
                    result.Recommendations.Add("Review content for gender-neutral language");
                }
            }
        }

        private async Task CheckRacialBiasAsync(string content, BiasDetectionResult result)
        {
            var racialBiasPatterns = new[]
            {
                @"\b(black|african)\b.*\b(crime|violence|poverty)\b",
                @"\b(white|caucasian)\b.*\b(success|wealth|intelligence)\b",
                @"\b(asian)\b.*\b(math|science|hardworking)\b",
                @"\b(hispanic|latin)\b.*\b(immigration|labor)\b"
            };

            foreach (var pattern in racialBiasPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.BiasTypes.Add("Racial Bias");
                    result.BiasIndicators.Add($"Racial bias pattern detected: {pattern}");
                    result.Recommendations.Add("Review content for racial sensitivity");
                }
            }
        }

        private async Task CheckAgeBiasAsync(string content, BiasDetectionResult result)
        {
            var ageBiasPatterns = new[]
            {
                @"\b(old|elderly|senior)\b.*\b(slow|outdated|technology)\b",
                @"\b(young|youth)\b.*\b(inexperienced|naive|immature)\b",
                @"\b(millennial|gen z)\b.*\b(lazy|entitled|selfish)\b",
                @"\b(boomer|baby boomer)\b.*\b(stubborn|outdated|resistant)\b"
            };

            foreach (var pattern in ageBiasPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.BiasTypes.Add("Age Bias");
                    result.BiasIndicators.Add($"Age bias pattern detected: {pattern}");
                    result.Recommendations.Add("Review content for age-inclusive language");
                }
            }
        }

        private async Task CheckCulturalBiasAsync(string content, BiasDetectionResult result)
        {
            var culturalBiasPatterns = new[]
            {
                @"\b(western|american)\b.*\b(superior|advanced|civilized)\b",
                @"\b(eastern|asian)\b.*\b(mysterious|exotic|foreign)\b",
                @"\b(african)\b.*\b(primitive|backward|underdeveloped)\b",
                @"\b(middle eastern)\b.*\b(dangerous|violent|terrorist)\b"
            };

            foreach (var pattern in culturalBiasPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.BiasTypes.Add("Cultural Bias");
                    result.BiasIndicators.Add($"Cultural bias pattern detected: {pattern}");
                    result.Recommendations.Add("Review content for cultural sensitivity");
                }
            }
        }

        private async Task CheckSocioeconomicBiasAsync(string content, BiasDetectionResult result)
        {
            var socioeconomicBiasPatterns = new[]
            {
                @"\b(rich|wealthy)\b.*\b(smart|successful|hardworking)\b",
                @"\b(poor|poverty)\b.*\b(lazy|unmotivated|failure)\b",
                @"\b(educated|college)\b.*\b(superior|intelligent|better)\b",
                @"\b(uneducated|dropout)\b.*\b(inferior|stupid|worthless)\b"
            };

            foreach (var pattern in socioeconomicBiasPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.BiasTypes.Add("Socioeconomic Bias");
                    result.BiasIndicators.Add($"Socioeconomic bias pattern detected: {pattern}");
                    result.Recommendations.Add("Review content for socioeconomic sensitivity");
                }
            }
        }

        private double CalculateBiasScore(List<string> biasIndicators)
        {
            if (biasIndicators.Count == 0)
                return 0.0;

            // Simple scoring based on number of bias indicators
            var score = Math.Min(100.0, biasIndicators.Count * 20.0);
            return score;
        }
    }

    public partial class BiasDetectionResult
    {
        public double BiasScore { get; set; }
        public List<string> BiasTypes { get; set; } = new();
        public List<string> BiasIndicators { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime DetectionTime { get; set; }
    }
}
