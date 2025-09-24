using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Quality.Analyzers
{
    public class DocumentationAnalyzer
    {
        private readonly ILogger<DocumentationAnalyzer> _logger;

        public DocumentationAnalyzer(ILogger<DocumentationAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<double> AnalyzeAsync(string code, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing documentation aspects of code");

            // Basic documentation checks
            var hasXmlComments = code.Contains("///") || code.Contains("<summary>");
            var hasMethodComments = code.Contains("// Method:") || code.Contains("// Function:");
            var hasClassComments = code.Contains("// Class:") || code.Contains("// This class");
            var hasInlineComments = code.Split('\n').Count(line => line.Trim().StartsWith("//")) > 0;

            var documentationScore = 0.0;
            if (hasXmlComments) documentationScore += 30;
            if (hasMethodComments) documentationScore += 25;
            if (hasClassComments) documentationScore += 25;
            if (hasInlineComments) documentationScore += 20;

            return await Task.FromResult(documentationScore);
        }
    }
}
