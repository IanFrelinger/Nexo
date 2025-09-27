using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Infrastructure.Quality.Analyzers
{
    public partial class TestabilityAnalyzer
    {
        private readonly ILogger<TestabilityAnalyzer> _logger;

        public TestabilityAnalyzer(ILogger<TestabilityAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentScore> AnalyzeAsync(string code, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing testability aspects of code");

            var score = new ComponentScore
            {
                ComponentName = "Testability",
                AnalyzedAt = DateTime.UtcNow
            };

            // Basic testability checks
            var hasDependencyInjection = code.Contains("I") && (code.Contains("Service") || code.Contains("Repository"));
            var hasPublicMethods = code.Contains("public ");
            var hasReturnValues = code.Contains("return ");
            var hasNoStaticDependencies = !code.Contains("DateTime.Now") && !code.Contains("Guid.NewGuid()");

            var testabilityScore = 0.0;
            if (hasDependencyInjection) testabilityScore += 25;
            if (hasPublicMethods) testabilityScore += 25;
            if (hasReturnValues) testabilityScore += 25;
            if (hasNoStaticDependencies) testabilityScore += 25;

            score.OverallScore = testabilityScore;
            score.Details = new Dictionary<string, object>
            {
                ["DependencyInjection"] = hasDependencyInjection,
                ["PublicMethods"] = hasPublicMethods,
                ["ReturnValues"] = hasReturnValues,
                ["NoStaticDependencies"] = hasNoStaticDependencies
            };

            return await Task.FromResult(score);
        }
    }
}
