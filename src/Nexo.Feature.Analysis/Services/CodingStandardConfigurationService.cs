using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for managing coding standards configurations.
    /// </summary>
    public class CodingStandardConfigurationService : ICodingStandardConfigurationService
    {
        private readonly ILogger<CodingStandardConfigurationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly List<CodingStandardConfiguration> _configurationHistory;

        public CodingStandardConfigurationService(ILogger<CodingStandardConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            _configurationHistory = new List<CodingStandardConfiguration>();
        }

        public CodingStandardConfiguration GetCurrentConfiguration()
        {
            return GetDefaultConfiguration();
        }

        public async Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating coding standards configuration");

            // Validate the configuration
            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
            }

            // Add to history
            _configurationHistory.Add(configuration);
            if (_configurationHistory.Count > 10) // Keep only last 10 configurations
            {
                _configurationHistory.RemoveAt(0);
            }

            await Task.CompletedTask;
            _logger.LogInformation("Coding standards configuration updated successfully");
        }

        public async Task<CodingStandardConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading coding standards configuration from file {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            return await LoadFromJsonAsync(jsonContent, cancellationToken);
        }

        public async Task SaveToFileAsync(CodingStandardConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving coding standards configuration to file {FilePath}", filePath);

            var jsonContent = await ToJsonAsync(configuration, cancellationToken);
            await File.WriteAllTextAsync(filePath, jsonContent, cancellationToken);
        }

        public async Task<CodingStandardConfiguration> LoadFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading coding standards configuration from JSON");

            try
            {
                var configuration = JsonSerializer.Deserialize<CodingStandardConfiguration>(jsonContent, _jsonOptions);
                if (configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration from JSON");
                }

                // Validate the loaded configuration
                var validationResult = ValidateConfiguration(configuration);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Invalid configuration loaded from JSON: {string.Join(", ", validationResult.Errors)}");
                }

                await Task.CompletedTask;
                return configuration;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing configuration JSON");
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }
        }

        public async Task<string> ToJsonAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Converting coding standards configuration to JSON");

            try
            {
                var json = JsonSerializer.Serialize(configuration, _jsonOptions);
                await Task.CompletedTask;
                return json;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error serializing configuration to JSON");
                throw new InvalidOperationException($"Failed to serialize configuration: {ex.Message}", ex);
            }
        }

        public CodingStandardConfigurationValidationResult ValidateConfiguration(CodingStandardConfiguration configuration)
        {
            var result = new CodingStandardConfigurationValidationResult
            {
                IsValid = true
            };

            try
            {
                // Validate basic properties
                if (string.IsNullOrWhiteSpace(configuration.Id))
                {
                    result.Errors.Add("Configuration ID is required");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(configuration.Name))
                {
                    result.Errors.Add("Configuration name is required");
                    result.IsValid = false;
                }

                // Validate standards
                foreach (var standard in configuration.Standards)
                {
                    ValidateStandard(standard, result);
                }

                // Validate global settings
                ValidateGlobalSettings(configuration.GlobalSettings, result);

                // Validate agent settings
                foreach (var agentSetting in configuration.AgentSettings.Values)
                {
                    ValidateAgentSettings(agentSetting, result);
                }

                // Validate file type settings
                foreach (var fileTypeSetting in configuration.FileTypeSettings.Values)
                {
                    ValidateFileTypeSettings(fileTypeSetting, result);
                }

                // Generate summary
                if (result.IsValid)
                {
                    result.Summary = $"Configuration '{configuration.Name}' is valid with {configuration.Standards.Count} standards and {configuration.Standards.Sum(s => s.Rules.Count)} rules";
                }
                else
                {
                    result.Summary = $"Configuration '{configuration.Name}' has {result.Errors.Count} errors and {result.Warnings.Count} warnings";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
                result.Summary = "Configuration validation failed due to an unexpected error";
            }

            return result;
        }

        public CodingStandardConfiguration GetDefaultConfiguration()
        {
            return new CodingStandardConfiguration
            {
                Id = "default",
                Name = "Default Coding Standards",
                Description = "Default coding standards configuration for the Nexo framework",
                Version = "1.0.0",
                IsEnabled = true,
                Standards = GetDefaultStandards(),
                GlobalSettings = GetDefaultGlobalSettings(),
                AgentSettings = GetDefaultAgentSettings(),
                FileTypeSettings = GetDefaultFileTypeSettings()
            };
        }

        public Dictionary<string, CodingStandardConfiguration> GetPredefinedConfigurations()
        {
            return new Dictionary<string, CodingStandardConfiguration>
            {
                ["default"] = GetDefaultConfiguration(),
                ["strict"] = GetStrictConfiguration(),
                ["relaxed"] = GetRelaxedConfiguration(),
                ["security-focused"] = GetSecurityFocusedConfiguration(),
                ["performance-focused"] = GetPerformanceFocusedConfiguration()
            };
        }

        public async Task ResetToDefaultAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resetting coding standards configuration to default");

            var defaultConfig = GetDefaultConfiguration();
            await UpdateConfigurationAsync(defaultConfig, cancellationToken);
        }

        public async Task<List<CodingStandardConfiguration>> GetConfigurationHistoryAsync()
        {
            return await Task.FromResult(_configurationHistory.ToList());
        }

        public async Task RestoreFromHistoryAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Restoring coding standards configuration from history: {ConfigurationId}", configurationId);

            var configuration = _configurationHistory.FirstOrDefault(c => c.Id == configurationId);
            if (configuration == null)
            {
                throw new ArgumentException($"Configuration with ID '{configurationId}' not found in history");
            }

            await UpdateConfigurationAsync(configuration, cancellationToken);
        }

        private void ValidateStandard(CodingStandard standard, CodingStandardConfigurationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(standard.Id))
            {
                result.Errors.Add($"Standard ID is required for standard '{standard.Name}'");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(standard.Name))
            {
                result.Errors.Add("Standard name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(standard.Language))
            {
                result.Warnings.Add($"Language not specified for standard '{standard.Name}'");
            }

            // Validate rules
            foreach (var rule in standard.Rules)
            {
                ValidateRule(rule, standard.Name, result);
            }
        }

        private void ValidateRule(CodingStandardRule rule, string standardName, CodingStandardConfigurationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                result.Errors.Add($"Rule ID is required for rule in standard '{standardName}'");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                result.Errors.Add($"Rule name is required for rule in standard '{standardName}'");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(rule.Pattern) && rule.Type == CodingStandardRuleType.Pattern)
            {
                result.Errors.Add($"Pattern is required for pattern rule '{rule.Name}' in standard '{standardName}'");
                result.IsValid = false;
            }

            // Validate regex pattern if it's a pattern rule
            if (rule.Type == CodingStandardRuleType.Pattern && !string.IsNullOrWhiteSpace(rule.Pattern))
            {
                try
                {
                    new System.Text.RegularExpressions.Regex(rule.Pattern);
                }
                catch (ArgumentException ex)
                {
                    result.Errors.Add($"Invalid regex pattern for rule '{rule.Name}' in standard '{standardName}': {ex.Message}");
                    result.IsValid = false;
                }
            }
        }

        private void ValidateGlobalSettings(CodingStandardGlobalSettings settings, CodingStandardConfigurationValidationResult result)
        {
            if (settings.MaxViolationsAllowed < 0)
            {
                result.Errors.Add("MaxViolationsAllowed must be non-negative");
                result.IsValid = false;
            }

            if (settings.MinimumQualityScore < 0 || settings.MinimumQualityScore > 100)
            {
                result.Errors.Add("MinimumQualityScore must be between 0 and 100");
                result.IsValid = false;
            }

            if (settings.ValidationTimeoutMs <= 0)
            {
                result.Errors.Add("ValidationTimeoutMs must be positive");
                result.IsValid = false;
            }
        }

        private void ValidateAgentSettings(CodingStandardAgentSettings settings, CodingStandardConfigurationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(settings.AgentId))
            {
                result.Errors.Add("Agent ID is required for agent settings");
                result.IsValid = false;
            }
        }

        private void ValidateFileTypeSettings(CodingStandardFileTypeSettings settings, CodingStandardConfigurationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(settings.FilePattern))
            {
                result.Errors.Add("File pattern is required for file type settings");
                result.IsValid = false;
            }
        }

        private List<CodingStandard> GetDefaultStandards()
        {
            return new List<CodingStandard>
            {
                new CodingStandard
                {
                    Id = "csharp-basic",
                    Name = "C# Basic Standards",
                    Description = "Basic coding standards for C# code",
                    Language = "csharp",
                    Framework = "dotnet",
                    IsEnabled = true,
                    Priority = 1,
                    Rules = new List<CodingStandardRule>
                    {
                        new CodingStandardRule
                        {
                            Id = "naming-classes",
                            Name = "Class Naming Convention",
                            Description = "Classes should use PascalCase naming",
                            Category = "Naming",
                            Severity = CodingStandardSeverity.Warning,
                            Type = CodingStandardRuleType.Naming,
                            Pattern = @"^[A-Z][a-zA-Z0-9]*$",
                            ErrorMessage = "Class names should use PascalCase",
                            SuggestedFix = "Use PascalCase for class names (e.g., MyClass instead of myClass)",
                            IsEnabled = true,
                            FilePatterns = new List<string> { "*.cs" }
                        },
                        new CodingStandardRule
                        {
                            Id = "naming-methods",
                            Name = "Method Naming Convention",
                            Description = "Methods should use PascalCase naming",
                            Category = "Naming",
                            Severity = CodingStandardSeverity.Warning,
                            Type = CodingStandardRuleType.Naming,
                            Pattern = @"^[A-Z][a-zA-Z0-9]*$",
                            ErrorMessage = "Method names should use PascalCase",
                            SuggestedFix = "Use PascalCase for method names (e.g., MyMethod instead of myMethod)",
                            IsEnabled = true,
                            FilePatterns = new List<string> { "*.cs" }
                        },
                        new CodingStandardRule
                        {
                            Id = "no-trailing-whitespace",
                            Name = "No Trailing Whitespace",
                            Description = "Lines should not have trailing whitespace",
                            Category = "Formatting",
                            Severity = CodingStandardSeverity.Info,
                            Type = CodingStandardRuleType.Formatting,
                            Pattern = "no-trailing-whitespace",
                            ErrorMessage = "Line contains trailing whitespace",
                            SuggestedFix = "Remove trailing whitespace",
                            IsEnabled = true,
                            FilePatterns = new List<string> { "*.cs", "*.js", "*.ts" }
                        },
                        new CodingStandardRule
                        {
                            Id = "max-line-length",
                            Name = "Maximum Line Length",
                            Description = "Lines should not exceed 120 characters",
                            Category = "Formatting",
                            Severity = CodingStandardSeverity.Warning,
                            Type = CodingStandardRuleType.Formatting,
                            Pattern = "max-line-length",
                            ErrorMessage = "Line length exceeds maximum allowed length",
                            SuggestedFix = "Break line into multiple lines",
                            IsEnabled = true,
                            FilePatterns = new List<string> { "*.cs", "*.js", "*.ts" },
                            Parameters = new Dictionary<string, object> { ["maxLength"] = 120 }
                        }
                    }
                }
            };
        }

        private CodingStandardGlobalSettings GetDefaultGlobalSettings()
        {
            return new CodingStandardGlobalSettings
            {
                FailOnCriticalViolations = true,
                FailOnErrorViolations = false,
                MaxViolationsAllowed = 10,
                MinimumQualityScore = 80,
                AutoFixEnabled = false,
                ValidationTimeoutMs = 30000,
                IncludeSuggestions = true,
                VerbosityLevel = CodingStandardVerbosityLevel.Normal,
                IncludePatterns = new List<string> { "*.cs", "*.js", "*.ts", "*.py", "*.java" },
                ExcludePatterns = new List<string> { "*.generated.cs", "*.designer.cs", "bin/**", "obj/**" }
            };
        }

        private Dictionary<string, CodingStandardAgentSettings> GetDefaultAgentSettings()
        {
            return new Dictionary<string, CodingStandardAgentSettings>
            {
                ["code-generation-agent"] = new CodingStandardAgentSettings
                {
                    AgentId = "code-generation-agent",
                    IsEnabled = true,
                    SeverityThreshold = CodingStandardSeverity.Warning,
                    AutoFixEnabled = true
                },
                ["domain-analysis-agent"] = new CodingStandardAgentSettings
                {
                    AgentId = "domain-analysis-agent",
                    IsEnabled = true,
                    SeverityThreshold = CodingStandardSeverity.Error,
                    AutoFixEnabled = false
                }
            };
        }

        private Dictionary<string, CodingStandardFileTypeSettings> GetDefaultFileTypeSettings()
        {
            return new Dictionary<string, CodingStandardFileTypeSettings>
            {
                [".cs"] = new CodingStandardFileTypeSettings
                {
                    FilePattern = "*.cs",
                    IsEnabled = true,
                    SeverityThreshold = CodingStandardSeverity.Warning
                },
                [".js"] = new CodingStandardFileTypeSettings
                {
                    FilePattern = "*.js",
                    IsEnabled = true,
                    SeverityThreshold = CodingStandardSeverity.Warning
                },
                [".ts"] = new CodingStandardFileTypeSettings
                {
                    FilePattern = "*.ts",
                    IsEnabled = true,
                    SeverityThreshold = CodingStandardSeverity.Warning
                }
            };
        }

        private CodingStandardConfiguration GetStrictConfiguration()
        {
            var config = GetDefaultConfiguration();
            config.Id = "strict";
            config.Name = "Strict Coding Standards";
            config.Description = "Strict coding standards with high quality requirements";
            config.GlobalSettings.MinimumQualityScore = 95;
            config.GlobalSettings.FailOnErrorViolations = true;
            config.GlobalSettings.MaxViolationsAllowed = 3;
            return config;
        }

        private CodingStandardConfiguration GetRelaxedConfiguration()
        {
            var config = GetDefaultConfiguration();
            config.Id = "relaxed";
            config.Name = "Relaxed Coding Standards";
            config.Description = "Relaxed coding standards with lower quality requirements";
            config.GlobalSettings.MinimumQualityScore = 60;
            config.GlobalSettings.FailOnErrorViolations = false;
            config.GlobalSettings.MaxViolationsAllowed = 25;
            return config;
        }

        private CodingStandardConfiguration GetSecurityFocusedConfiguration()
        {
            var config = GetDefaultConfiguration();
            config.Id = "security-focused";
            config.Name = "Security-Focused Coding Standards";
            config.Description = "Coding standards focused on security best practices";
            
            // Add security-specific rules
            var securityStandard = new CodingStandard
            {
                Id = "security-rules",
                Name = "Security Rules",
                Description = "Security-focused coding standards",
                Language = "csharp",
                IsEnabled = true,
                Priority = 10,
                Rules = new List<CodingStandardRule>
                {
                    new CodingStandardRule
                    {
                        Id = "no-hardcoded-passwords",
                        Name = "No Hardcoded Passwords",
                        Description = "No hardcoded passwords in code",
                        Category = "Security",
                        Severity = CodingStandardSeverity.Critical,
                        Type = CodingStandardRuleType.Security,
                        Pattern = @"password\s*=\s*[""'][^""']*[""']",
                        ErrorMessage = "Hardcoded password detected",
                        SuggestedFix = "Use secure configuration or environment variables for passwords",
                        IsEnabled = true,
                        FilePatterns = new List<string> { "*.cs", "*.js", "*.ts" }
                    }
                }
            };
            
            config.Standards.Add(securityStandard);
            return config;
        }

        private CodingStandardConfiguration GetPerformanceFocusedConfiguration()
        {
            var config = GetDefaultConfiguration();
            config.Id = "performance-focused";
            config.Name = "Performance-Focused Coding Standards";
            config.Description = "Coding standards focused on performance optimization";
            
            // Add performance-specific rules
            var performanceStandard = new CodingStandard
            {
                Id = "performance-rules",
                Name = "Performance Rules",
                Description = "Performance-focused coding standards",
                Language = "csharp",
                IsEnabled = true,
                Priority = 10,
                Rules = new List<CodingStandardRule>
                {
                    new CodingStandardRule
                    {
                        Id = "avoid-string-concatenation",
                        Name = "Avoid String Concatenation",
                        Description = "Avoid string concatenation in loops",
                        Category = "Performance",
                        Severity = CodingStandardSeverity.Warning,
                        Type = CodingStandardRuleType.Performance,
                        Pattern = @"for\s*\([^)]*\)\s*\{[^}]*string[^}]*\+[^}]*\}",
                        ErrorMessage = "String concatenation in loop detected",
                        SuggestedFix = "Use StringBuilder for string concatenation in loops",
                        IsEnabled = true,
                        FilePatterns = new List<string> { "*.cs" }
                    }
                }
            };
            
            config.Standards.Add(performanceStandard);
            return config;
        }
    }
}
