using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Infrastructure.Quality.Analyzers
{
    public partial class MaintainabilityAnalyzer
    {
        private readonly ILogger<MaintainabilityAnalyzer> _logger;

        public MaintainabilityAnalyzer(ILogger<MaintainabilityAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentScore> AnalyzeAsync(string code, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing maintainability aspects of code");

            var score = new ComponentScore
            {
                ComponentName = "Maintainability",
                AnalyzedAt = DateTime.UtcNow
            };

            // Basic maintainability checks
            var hasSingleResponsibility = CountClasses(code) <= 1 || CountMethods(code) <= 5;
            var hasLowComplexity = CalculateComplexity(code) <= 10;
            var hasGoodNaming = !code.Contains("var1") && !code.Contains("temp");
            var hasModularity = code.Contains("public") && code.Contains("private");

            var maintainabilityScore = 0.0;
            if (hasSingleResponsibility) maintainabilityScore += 25;
            if (hasLowComplexity) maintainabilityScore += 25;
            if (hasGoodNaming) maintainabilityScore += 25;
            if (hasModularity) maintainabilityScore += 25;

            score.OverallScore = maintainabilityScore;
            score.Details = new Dictionary<string, object>
            {
                ["SingleResponsibility"] = hasSingleResponsibility,
                ["LowComplexity"] = hasLowComplexity,
                ["GoodNaming"] = hasGoodNaming,
                ["Modularity"] = hasModularity
            };

            return await Task.FromResult(score);
        }

        private int CountClasses(string code) => code.Split(new[] { "class " }, StringSplitOptions.None).Length - 1;
        private int CountMethods(string code) => code.Split(new[] { "public ", "private ", "protected " }, StringSplitOptions.None).Count(s => s.Contains("("));
        private int CalculateComplexity(string code) => code.Split(new[] { "if", "while", "for", "switch" }, StringSplitOptions.None).Length - 1;
    }
}
