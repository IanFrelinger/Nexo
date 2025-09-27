using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Quality.Analyzers
{
    public partial class ReadabilityAnalyzer
    {
        private readonly ILogger<ReadabilityAnalyzer> _logger;

        public ReadabilityAnalyzer(ILogger<ReadabilityAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<double> AnalyzeAsync(string code, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing readability aspects of code");

            // Basic readability checks
            var hasComments = code.Contains("//") || code.Contains("/*");
            var hasGoodNaming = !code.Contains("var1") && !code.Contains("temp");
            var hasProperIndentation = code.Split('\n').All(line => line.StartsWith("    ") || line.Trim().Length == 0);
            var hasShortMethods = CountMethods(code) <= 10;

            var readabilityScore = 0.0;
            if (hasComments) readabilityScore += 25;
            if (hasGoodNaming) readabilityScore += 25;
            if (hasProperIndentation) readabilityScore += 25;
            if (hasShortMethods) readabilityScore += 25;

            return await Task.FromResult(readabilityScore);
        }

        private int CountMethods(string code) => code.Split(new[] { "public ", "private ", "protected " }, StringSplitOptions.None).Count(s => s.Contains("("));
    }
}
