using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonSchema.Net;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;
using Nexo.Core.Specs;
using YamlDotNet.Serialization;

namespace Nexo.Core.Validation
{
    /// <summary>
    /// Validates FeatureSpec records and JSON/YAML input against the JSON Schema.
    /// </summary>
    public class FeatureSpecValidator : ICompilationGate
    {
        private readonly ILogger<FeatureSpecValidator> _logger;
        private readonly JsonSchema _schema;
        private readonly JsonSerializerOptions _jsonOptions;

        public FeatureSpecValidator(ILogger<FeatureSpecValidator> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Load the JSON schema
            var schemaPath = Path.Combine("policies", "schemas", "featurespec.schema.json");
            if (File.Exists(schemaPath))
            {
                var schemaJson = File.ReadAllText(schemaPath);
                _schema = JsonSchema.FromText(schemaJson);
            }
            else
            {
                throw new InvalidOperationException($"Schema file not found: {schemaPath}");
            }
        }

        /// <summary>
        /// Validates a FeatureSpec record for basic invariants.
        /// </summary>
        /// <param name="featureSpec">The FeatureSpec to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateFeatureSpec(FeatureSpec featureSpec)
        {
            var findings = new List<string>();

            // Basic invariant checks
            if (string.IsNullOrWhiteSpace(featureSpec.Name))
            {
                findings.Add("Name is required and cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(featureSpec.Version))
            {
                findings.Add("Version is required and cannot be empty");
            }
            else if (!IsValidSemanticVersion(featureSpec.Version))
            {
                findings.Add($"Version '{featureSpec.Version}' is not a valid semantic version");
            }

            if (featureSpec.Capabilities == null || featureSpec.Capabilities.Length == 0)
            {
                findings.Add("At least one capability is required");
            }
            else if (featureSpec.Capabilities.Any(string.IsNullOrWhiteSpace))
            {
                findings.Add("Capabilities cannot contain empty or null values");
            }

            if (featureSpec.Parameters == null)
            {
                findings.Add("Parameters cannot be null (use empty dictionary if no parameters)");
            }
            else if (featureSpec.Parameters.Values.Any(string.IsNullOrWhiteSpace))
            {
                findings.Add("Parameter values cannot be empty or null");
            }

            if (featureSpec.Policies == null)
            {
                findings.Add("Policies cannot be null (use empty array if no policies)");
            }
            else if (featureSpec.Policies.Any(string.IsNullOrWhiteSpace))
            {
                findings.Add("Policies cannot contain empty or null values");
            }

            var passed = findings.Count == 0;
            return new ValidationResult(passed, "FeatureSpec Validation", findings);
        }

        /// <summary>
        /// Validates JSON/YAML input against the JSON Schema.
        /// </summary>
        /// <param name="input">The JSON or YAML input to validate</param>
        /// <param name="isYaml">Whether the input is YAML format</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateInput(string input, bool isYaml = false)
        {
            var findings = new List<string>();

            try
            {
                JsonElement jsonElement;

                if (isYaml)
                {
                    // Convert YAML to JSON
                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize(input);
                    
                    var serializer = new SerializerBuilder()
                        .JsonCompatible()
                        .Build();
                    var jsonString = serializer.Serialize(yamlObject);
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                }
                else
                {
                    // Parse JSON directly
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
                }

                // Validate against JSON Schema
                var validationResults = _schema.Validate(jsonElement);
                
                if (!validationResults.IsValid)
                {
                    foreach (var error in validationResults)
                    {
                        var path = string.IsNullOrEmpty(error.InstanceLocation) ? "root" : error.InstanceLocation;
                        findings.Add($"{path}: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                findings.Add($"Failed to parse input: {ex.Message}");
            }

            var passed = findings.Count == 0;
            return new ValidationResult(passed, "Schema Validation", findings);
        }

        /// <summary>
        /// Validates a file containing FeatureSpec data.
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Validation result</returns>
        public async Task<ValidationResult> ValidateFileAsync(string filePath, CancellationToken ct = default)
        {
            var findings = new List<string>();

            try
            {
                if (!File.Exists(filePath))
                {
                    findings.Add($"File not found: {filePath}");
                    return new ValidationResult(false, "File Validation", findings);
                }

                var content = await File.ReadAllTextAsync(filePath, ct);
                var isYaml = filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                           filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);

                // First validate against schema
                var schemaResult = ValidateInput(content, isYaml);
                findings.AddRange(schemaResult.Findings);

                // If schema validation passed, try to deserialize and validate the record
                if (schemaResult.Passed)
                {
                    try
                    {
                        FeatureSpec featureSpec;
                        
                        if (isYaml)
                        {
                            var deserializer = new DeserializerBuilder().Build();
                            featureSpec = deserializer.Deserialize<FeatureSpec>(content);
                        }
                        else
                        {
                            featureSpec = JsonSerializer.Deserialize<FeatureSpec>(content, _jsonOptions);
                        }

                        if (featureSpec != null)
                        {
                            var recordResult = ValidateFeatureSpec(featureSpec);
                            findings.AddRange(recordResult.Findings);
                        }
                        else
                        {
                            findings.Add("Failed to deserialize FeatureSpec from file");
                        }
                    }
                    catch (Exception ex)
                    {
                        findings.Add($"Failed to deserialize FeatureSpec: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                findings.Add($"Error reading file: {ex.Message}");
            }

            var passed = findings.Count == 0;
            return new ValidationResult(passed, "File Validation", findings);
        }

        /// <summary>
        /// ICompilationGate implementation - validates source code as FeatureSpec.
        /// </summary>
        public async ValueTask<ValidationResult> ValidateAsync(string sourceCode, CancellationToken ct = default)
        {
            return await Task.FromResult(ValidateInput(sourceCode, false));
        }

        private static bool IsValidSemanticVersion(string version)
        {
            // Basic semantic version pattern: major.minor.patch[-prerelease][+build]
            var pattern = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9\-]+)?(\+[a-zA-Z0-9\-]+)?$";
            return System.Text.RegularExpressions.Regex.IsMatch(version, pattern);
        }
    }
}
