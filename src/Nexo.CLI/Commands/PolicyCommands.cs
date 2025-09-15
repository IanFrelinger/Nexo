using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for policy management and validation
    /// </summary>
    public class PolicyCommands : ICommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PolicyCommands> _logger;

        public PolicyCommands(IServiceProvider serviceProvider, ILogger<PolicyCommands> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateCommand()
        {
            var policyCommand = new Command("policy", "Policy management and validation commands");

            // Safety scan command
            var safetyScanCommand = new Command("safety", "Run safety policy scan");
            var safetyPolicyArg = new Argument<string>("policy", "Path to safety policy file");
            var safetyOutputArg = new Argument<string>("output", "Output directory for safety report");
            var safetyFormatArg = new Option<string[]>("--format", "Output formats (json, md, sarif)") { AllowMultipleArgumentsPerToken = true };

            safetyScanCommand.AddArgument(safetyPolicyArg);
            safetyScanCommand.AddArgument(safetyOutputArg);
            safetyScanCommand.AddOption(safetyFormatArg);

            safetyScanCommand.SetHandler(async (policyPath, outputPath, formats) =>
            {
                await RunSafetyScanAsync(policyPath, outputPath, formats);
            }, safetyPolicyArg, safetyOutputArg, safetyFormatArg);

            // Quality run command
            var qualityRunCommand = new Command("quality", "Run quality policy validation");
            var qualityPolicyArg = new Argument<string>("policy", "Path to quality policy file");
            var qualityOutputArg = new Argument<string>("output", "Output directory for quality report");
            var qualityFormatArg = new Option<string[]>("--format", "Output formats (json, md, sarif)") { AllowMultipleArgumentsPerToken = true };

            qualityRunCommand.AddArgument(qualityPolicyArg);
            qualityRunCommand.AddArgument(qualityOutputArg);
            qualityRunCommand.AddOption(qualityFormatArg);

            qualityRunCommand.SetHandler(async (policyPath, outputPath, formats) =>
            {
                await RunQualityValidationAsync(policyPath, outputPath, formats);
            }, qualityPolicyArg, qualityOutputArg, qualityFormatArg);

            // Policy apply command
            var policyApplyCommand = new Command("apply", "Apply policy manifest");
            var manifestArg = new Argument<string>("manifest", "Path to policy manifest file");
            var policyOutputArg = new Argument<string>("output", "Output directory for policy report");
            var policyCodeArg = new Option<string>("--code", "Code to validate (if not provided, reads from stdin)");

            policyApplyCommand.AddArgument(manifestArg);
            policyApplyCommand.AddArgument(policyOutputArg);
            policyApplyCommand.AddOption(policyCodeArg);

            policyApplyCommand.SetHandler(async (manifestPath, outputPath, code) =>
            {
                await ApplyPolicyManifestAsync(manifestPath, outputPath, code);
            }, manifestArg, policyOutputArg, policyCodeArg);

            // Policy validate command
            var policyValidateCommand = new Command("validate", "Validate policy file against schema");
            var validatePolicyArg = new Argument<string>("policy", "Path to policy file");
            var validateSchemaArg = new Argument<string>("schema", "Path to JSON schema file");

            policyValidateCommand.AddArgument(validatePolicyArg);
            policyValidateCommand.AddArgument(validateSchemaArg);

            policyValidateCommand.SetHandler(async (policyPath, schemaPath) =>
            {
                await ValidatePolicyAsync(policyPath, schemaPath);
            }, validatePolicyArg, validateSchemaArg);

            policyCommand.AddCommand(safetyScanCommand);
            policyCommand.AddCommand(qualityRunCommand);
            policyCommand.AddCommand(policyApplyCommand);
            policyCommand.AddCommand(policyValidateCommand);

            return policyCommand;
        }

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

        private async Task<string> ReadCodeFromInputAsync()
        {
            // Check if there's input available
            if (Console.IsInputRedirected)
            {
                return await Console.In.ReadToEndAsync();
            }

            // Return sample code for demonstration
            return @"
using System;
public class SampleClass
{
    public string GetMessage() => ""Hello World"";
    public void ProcessData(string data)
    {
        Console.WriteLine(data);
    }
}";
        }

        private async Task GenerateSafetyReportAsync(Nexo.Core.Domain.Models.Policy.SafetyPolicyResult result, string outputPath, string format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileName = $"safety-report-{timestamp}.{format}";

            switch (format.ToLower())
            {
                case "json":
                    await GenerateJsonReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                case "md":
                    await GenerateMarkdownReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                case "sarif":
                    await GenerateSarifReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                default:
                    _logger.LogWarning("Unknown format: {Format}", format);
                    break;
            }
        }

        private async Task GenerateQualityReportAsync(Nexo.Core.Domain.Models.Policy.QualityPolicyResult result, string outputPath, string format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileName = format.ToLower() == "sarif" ? "quality.sarif" : $"quality-report-{timestamp}.{format}";

            switch (format.ToLower())
            {
                case "json":
                    await GenerateJsonReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                case "md":
                    await GenerateMarkdownReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                case "sarif":
                    await GenerateSarifReportAsync(result, Path.Combine(outputPath, fileName));
                    break;
                default:
                    _logger.LogWarning("Unknown format: {Format}", format);
                    break;
            }
        }

        private async Task GenerateJsonReportAsync(object result, string filePath)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        private async Task GenerateMarkdownReportAsync(object result, string filePath)
        {
            var markdown = GenerateMarkdownContent(result);
            await File.WriteAllTextAsync(filePath, markdown);
        }

        private async Task GenerateSarifReportAsync(object result, string filePath)
        {
            // Generate a basic SARIF report
            var sarifReport = new
            {
                version = "2.1.0",
                runs = new[]
                {
                    new
                    {
                        tool = new
                        {
                            driver = new
                            {
                                name = "Nexo Policy Engine",
                                version = "1.0.0",
                                informationUri = "https://github.com/IanFrelinger/Nexo"
                            }
                        },
                        results = new object[0], // Empty results for now
                        invocations = new[]
                        {
                            new
                            {
                                executionSuccessful = true,
                                exitCode = 0,
                                startTimeUtc = DateTime.UtcNow.ToString("O")
                            }
                        }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(sarifReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        private string GenerateMarkdownContent(object result)
        {
            return $@"
# Policy Report

Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

## Summary

{result}

---
*Generated by Nexo Policy Engine*
";
        }

        private void DisplaySafetySummary(Nexo.Core.Domain.Models.Policy.SafetyPolicyResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Security Safety Scan Summary");
            Console.WriteLine("=====================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Safety Score: {result.SafetyScore:F1}/10.0");
            Console.WriteLine($"Violations: {result.Violations.Count}");

            if (result.Violations.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Violations:");
                foreach (var violation in result.Violations)
                {
                    var icon = violation.Severity switch
                    {
                        "block" => "BLOCKED",
                        "error" => "ERROR:",
                        "warn" => "WARNING:",
                        "info" => "INFO:",
                        _ => "•"
                    };
                    Console.WriteLine($"  {icon} [{violation.Severity.ToUpper()}] {violation.Description}");
                    Console.WriteLine($"      {violation.Message}");
                }
            }
        }

        private void DisplayQualitySummary(Nexo.Core.Domain.Models.Policy.QualityPolicyResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Stats Quality Validation Summary");
            Console.WriteLine("=============================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Quality Score: {result.QualityScore:F2}");
            Console.WriteLine($"Violations: {result.Violations.Count}");

            if (result.GateScores.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Gate Scores:");
                foreach (var gate in result.GateScores)
                {
                    Console.WriteLine($"  {gate.Key}: {gate.Value:P1}");
                }
            }

            if (result.Violations.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Violations:");
                foreach (var violation in result.Violations)
                {
                    var icon = violation.Severity switch
                    {
                        "error" => "ERROR:",
                        "warn" => "WARNING:",
                        "info" => "INFO:",
                        _ => "•"
                    };
                    Console.WriteLine($"  {icon} [{violation.Severity.ToUpper()}] {violation.Description}");
                    Console.WriteLine($"      {violation.Message}");
                }
            }
        }

        private void DisplayPolicySummary(Nexo.Core.Domain.Models.Policy.PolicyExecutionResult result)
        {
            Console.WriteLine();
            Console.WriteLine("List Policy Execution Summary");
            Console.WriteLine("============================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Report: {result.ReportPath}");

            if (result.SafetyResult != null)
            {
                Console.WriteLine($"Safety Score: {result.SafetyResult.SafetyScore:F1}/10.0");
            }

            if (result.QualityResult != null)
            {
                Console.WriteLine($"Quality Score: {result.QualityResult.QualityScore:F2}");
            }

            if (result.Errors.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ERROR: {error}");
                }
            }
        }
    }
}
