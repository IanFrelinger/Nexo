using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Policy execution functionality
    /// </summary>
    public partial class PolicyCommands
    {
        private async Task RunSafetyScanAsync(string policyPath, string outputPath, string[] formats)
        {
            try
            {
                _logger.LogInformation("Running safety scan with policy: {PolicyPath}", policyPath);

                var policyEngine = _serviceProvider.GetRequiredService<IPolicyEngine>();
                var policy = await policyEngine.LoadPolicyAsync(policyPath);

                if (policy.Safety == null)
                {
                    Console.WriteLine("ERROR: No safety policy found in the specified file");
                    return;
                }

                // Read code from stdin or use sample code
                var code = await ReadCodeFromInputAsync();

                var result = await policyEngine.ApplySafetyPolicyAsync(code, policy.Safety);

                // Create output directory
                Directory.CreateDirectory(outputPath);

                // Generate reports in requested formats
                foreach (var format in formats)
                {
                    await GenerateSafetyReportAsync(result, outputPath, format);
                }

                // Display summary
                DisplaySafetySummary(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running safety scan");
                Console.WriteLine($"ERROR: Error running safety scan: {ex.Message}");
            }
        }

        private async Task RunQualityValidationAsync(string policyPath, string outputPath, string[] formats)
        {
            try
            {
                _logger.LogInformation("Running quality validation with policy: {PolicyPath}", policyPath);

                var policyEngine = _serviceProvider.GetRequiredService<IPolicyEngine>();
                var policy = await policyEngine.LoadPolicyAsync(policyPath);

                if (policy.Quality == null)
                {
                    Console.WriteLine("ERROR: No quality policy found in the specified file");
                    return;
                }

                // Read code from stdin or use sample code
                var code = await ReadCodeFromInputAsync();

                var result = await policyEngine.ApplyQualityPolicyAsync(code, policy.Quality);

                // Create output directory
                Directory.CreateDirectory(outputPath);

                // Generate reports in requested formats
                foreach (var format in formats)
                {
                    await GenerateQualityReportAsync(result, outputPath, format);
                }

                // Display summary
                DisplayQualitySummary(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quality validation");
                Console.WriteLine($"ERROR: Error running quality validation: {ex.Message}");
            }
        }

        private async Task ApplyPolicyManifestAsync(string manifestPath, string outputPath, string? code)
        {
            try
            {
                _logger.LogInformation("Applying policy manifest: {ManifestPath}", manifestPath);

                var policyEngine = _serviceProvider.GetRequiredService<IPolicyEngine>();
                
                // Read code from parameter or stdin
                var codeToValidate = code ?? await ReadCodeFromInputAsync();

                var result = await policyEngine.ExecutePolicyManifestAsync(manifestPath, codeToValidate);

                // Create output directory
                Directory.CreateDirectory(outputPath);

                // Display summary
                DisplayPolicySummary(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying policy manifest");
                Console.WriteLine($"ERROR: Error applying policy manifest: {ex.Message}");
            }
        }

        private async Task ValidatePolicyAsync(string policyPath, string schemaPath)
        {
            try
            {
                _logger.LogInformation("Validating policy: {PolicyPath} against schema: {SchemaPath}", policyPath, schemaPath);

                var policyEngine = _serviceProvider.GetRequiredService<IPolicyEngine>();
                var result = await policyEngine.ValidatePolicyAsync(policyPath, schemaPath);

                if (result.IsValid)
                {
                    Console.WriteLine("SUCCESS: Policy validation passed");
                }
                else
                {
                    Console.WriteLine("ERROR: Policy validation failed:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }

                if (result.Warnings.Any())
                {
                    Console.WriteLine("WARNINGS:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating policy");
                Console.WriteLine($"ERROR: Error validating policy: {ex.Message}");
            }
        }
    }
}
