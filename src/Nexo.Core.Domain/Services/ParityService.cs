using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for checking parity between different providers and outputs
    /// </summary>
    public partial class ParityService : IParityService
    {
        private readonly ILogger<ParityService> _logger;
        private readonly Dictionary<string, ParityThreshold> _thresholds;

        public ParityService(ILogger<ParityService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _thresholds = new Dictionary<string, ParityThreshold>
            {
                ["default"] = new ParityThreshold { MinSimilarity = 0.85, MaxDifference = 0.15 },
                ["labels"] = new ParityThreshold { MinSimilarity = 0.80, MaxDifference = 0.20 },
                ["summary"] = new ParityThreshold { MinSimilarity = 0.85, MaxDifference = 0.15 },
                ["code"] = new ParityThreshold { MinSimilarity = 0.90, MaxDifference = 0.10 }
            };
        }

        public async Task<ParityResult> CompareOutputsAsync(
            string output1,
            string output2,
            string contentType = "default",
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Comparing outputs for content type: {ContentType}", contentType);

            var threshold = GetThreshold(contentType);
            var similarity = CalculateSimilarity(output1, output2);
            var difference = 1.0 - similarity;

            var result = new ParityResult
            {
                Output1 = output1,
                Output2 = output2,
                Similarity = similarity,
                Difference = difference,
                Threshold = threshold,
                Passed = similarity >= threshold.MinSimilarity && difference <= threshold.MaxDifference,
                ContentType = contentType,
                ComparedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Parity comparison result: Similarity={Similarity:F3}, Threshold={Threshold:F3}, Passed={Passed}", 
                similarity, threshold.MinSimilarity, result.Passed);

            return await Task.FromResult(result);
        }

        public async Task<ParityResult> CompareProviderOutputsAsync(
            string provider1,
            string output1,
            string provider2,
            string output2,
            string contentType = "default",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Comparing outputs between providers {Provider1} and {Provider2}", provider1, provider2);

            var result = await CompareOutputsAsync(output1, output2, contentType, cancellationToken);
            result.Provider1 = provider1;
            result.Provider2 = provider2;

            return result;
        }

        public async Task<List<ParityResult>> CompareMultipleOutputsAsync(
            List<ProviderOutput> outputs,
            string contentType = "default",
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Comparing {Count} outputs for content type: {ContentType}", outputs.Count, contentType);

            var results = new List<ParityResult>();

            for (int i = 0; i < outputs.Count; i++)
            {
                for (int j = i + 1; j < outputs.Count; j++)
                {
                    var result = await CompareProviderOutputsAsync(
                        outputs[i].Provider,
                        outputs[i].Output,
                        outputs[j].Provider,
                        outputs[j].Output,
                        contentType,
                        cancellationToken);

                    results.Add(result);
                }
            }

            return results;
        }

        public async Task<bool> SetParityThresholdAsync(
            string contentType,
            double minSimilarity,
            double maxDifference,
            CancellationToken cancellationToken = default)
        {
            if (minSimilarity < 0 || minSimilarity > 1)
                throw new ArgumentException("MinSimilarity must be between 0 and 1", nameof(minSimilarity));

            if (maxDifference < 0 || maxDifference > 1)
                throw new ArgumentException("MaxDifference must be between 0 and 1", nameof(maxDifference));

            if (minSimilarity + maxDifference > 1)
                throw new ArgumentException("MinSimilarity + MaxDifference cannot exceed 1");

            _thresholds[contentType] = new ParityThreshold
            {
                MinSimilarity = minSimilarity,
                MaxDifference = maxDifference
            };

            _logger.LogInformation("Updated parity threshold for {ContentType}: MinSimilarity={MinSimilarity:F3}, MaxDifference={MaxDifference:F3}", 
                contentType, minSimilarity, maxDifference);

            return await Task.FromResult(true);
        }

        public async Task<ParityThreshold> GetParityThresholdAsync(
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var threshold = GetThreshold(contentType);
            return await Task.FromResult(threshold);
        }

        public async Task<bool> ValidateProviderSwitchingAsync(
            string originalProvider,
            string newProvider,
            string originalOutput,
            string newOutput,
            string contentType = "default",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating provider switching from {OriginalProvider} to {NewProvider}", 
                originalProvider, newProvider);

            var result = await CompareProviderOutputsAsync(
                originalProvider, originalOutput,
                newProvider, newOutput,
                contentType, cancellationToken);

            if (result.Passed)
            {
                _logger.LogInformation("Provider switching validation passed: Similarity={Similarity:F3}", result.Similarity);
            }
            else
            {
                _logger.LogWarning("Provider switching validation failed: Similarity={Similarity:F3}, Threshold={Threshold:F3}", 
                    result.Similarity, result.Threshold.MinSimilarity);
            }

            return result.Passed;
        }

        private ParityThreshold GetThreshold(string contentType)
        {
            return _thresholds.TryGetValue(contentType, out var threshold) 
                ? threshold 
                : _thresholds["default"];
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) && string.IsNullOrEmpty(text2))
                return 1.0;

            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            // Normalize texts
            var normalized1 = NormalizeText(text1);
            var normalized2 = NormalizeText(text2);

            // Calculate Jaccard similarity
            var tokens1 = Tokenize(normalized1);
            var tokens2 = Tokenize(normalized2);

            var intersection = tokens1.Intersect(tokens2).Count();
            var union = tokens1.Union(tokens2).Count();

            return union == 0 ? 1.0 : (double)intersection / union;
        }

        private string NormalizeText(string text)
        {
            return text.ToLowerInvariant()
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private HashSet<string> Tokenize(string text)
        {
            return text.Split(new[] { ' ', '\t', '\n', ',', '.', ';', '!', '?', '(', ')', '[', ']', '{', '}', '/', ':', '\\' },
                StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
        }
    }

    /// <summary>
    /// Parity threshold configuration
    /// </summary>
    public partial class ParityThreshold
    {
        public double MinSimilarity { get; set; } = 0.85;
        public double MaxDifference { get; set; } = 0.15;
    }

    /// <summary>
    /// Provider output
    /// </summary>
    public partial class ProviderOutput
    {
        public string Provider { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Parity comparison result
    /// </summary>
    public partial class ParityResult
    {
        public string Output1 { get; set; } = string.Empty;
        public string Output2 { get; set; } = string.Empty;
        public string? Provider1 { get; set; }
        public string? Provider2 { get; set; }
        public double Similarity { get; set; }
        public double Difference { get; set; }
        public ParityThreshold Threshold { get; set; } = new();
        public bool Passed { get; set; }
        public string ContentType { get; set; } = "default";
        public DateTime ComparedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interface for parity service
    /// </summary>
    public partial interface IParityService
    {
        Task<ParityResult> CompareOutputsAsync(
            string output1,
            string output2,
            string contentType = "default",
            CancellationToken cancellationToken = default);

        Task<ParityResult> CompareProviderOutputsAsync(
            string provider1,
            string output1,
            string provider2,
            string output2,
            string contentType = "default",
            CancellationToken cancellationToken = default);

        Task<List<ParityResult>> CompareMultipleOutputsAsync(
            List<ProviderOutput> outputs,
            string contentType = "default",
            CancellationToken cancellationToken = default);

        Task<bool> SetParityThresholdAsync(
            string contentType,
            double minSimilarity,
            double maxDifference,
            CancellationToken cancellationToken = default);

        Task<ParityThreshold> GetParityThresholdAsync(
            string contentType,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateProviderSwitchingAsync(
            string originalProvider,
            string newProvider,
            string originalOutput,
            string newOutput,
            string contentType = "default",
            CancellationToken cancellationToken = default);
    }
}
