using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Configuration validation functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        /// <summary>
        /// Validates the test runner configuration.
        /// </summary>
        public async Task<TestValidationResult> ValidateConfigurationAsync(TestConfiguration configuration)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                if (configuration == null)
                {
                    errors.Add("Test configuration is null");
                    return new TestValidationResult(false, errors, warnings, TimeSpan.Zero);
                }

                // Validate timeout configuration
                if (configuration.DefaultTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Default timeout must be greater than zero");
                }

                if (configuration.AiConnectivityTimeout <= TimeSpan.Zero)
                {
                    errors.Add("AI connectivity timeout must be greater than zero");
                }

                if (configuration.DomainAnalysisTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Domain analysis timeout must be greater than zero");
                }

                if (configuration.CodeGenerationTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Code generation timeout must be greater than zero");
                }

                if (configuration.EndToEndTimeout <= TimeSpan.Zero)
                {
                    errors.Add("End-to-end timeout must be greater than zero");
                }

                if (configuration.PerformanceTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Performance timeout must be greater than zero");
                }

                // Validate output directory
                if (string.IsNullOrWhiteSpace(configuration.OutputDirectory))
                {
                    errors.Add("Output directory cannot be empty");
                }

                // Check if we can discover tests
                var discoveredTests = await DiscoverTestsAsync();
                if (!discoveredTests.Any())
                {
                    warnings.Add("No tests were discovered");
                }

                return new TestValidationResult(errors.Count == 0, errors, warnings, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                return new TestValidationResult(false, errors, warnings, TimeSpan.Zero);
            }
        }
    }
}
