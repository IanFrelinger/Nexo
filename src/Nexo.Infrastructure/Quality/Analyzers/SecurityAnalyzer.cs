using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Infrastructure.Quality.Analyzers
{
    public partial class SecurityAnalyzer
    {
        private readonly ILogger<SecurityAnalyzer> _logger;

        public SecurityAnalyzer(ILogger<SecurityAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentScore> AnalyzeAsync(string code, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing security aspects of code");

            var score = new ComponentScore
            {
                ComponentName = "Security",
                AnalyzedAt = DateTime.UtcNow
            };

            // Basic security checks
            var hasInputValidation = code.Contains("Validate") || code.Contains("Sanitize");
            var hasAuthentication = code.Contains("Authenticate") || code.Contains("Authorize");
            var hasEncryption = code.Contains("Encrypt") || code.Contains("Hash");
            var hasSecureDefaults = !code.Contains("password") || code.Contains("Password");

            var securityScore = 0.0;
            if (hasInputValidation) securityScore += 25;
            if (hasAuthentication) securityScore += 25;
            if (hasEncryption) securityScore += 25;
            if (hasSecureDefaults) securityScore += 25;

            score.OverallScore = securityScore;
            score.Details = new Dictionary<string, object>
            {
                ["InputValidation"] = hasInputValidation,
                ["Authentication"] = hasAuthentication,
                ["Encryption"] = hasEncryption,
                ["SecureDefaults"] = hasSecureDefaults
            };

            return await Task.FromResult(score);
        }
    }
}
