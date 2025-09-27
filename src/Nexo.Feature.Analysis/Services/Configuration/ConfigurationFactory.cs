using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.Configuration;

/// <summary>
/// Creates predefined coding standard configurations.
/// </summary>
public partial class ConfigurationFactory
{
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
