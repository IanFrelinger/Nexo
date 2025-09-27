using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Security analysis functionality for SecurityAnalysisAgent.
/// </summary>
public partial class SecurityAnalysisAgent
{
    /// <summary>
    /// Analyzes security vulnerabilities in the request
    /// </summary>
    private async Task<SecurityAnalysis> AnalyzeSecurityVulnerabilities(AgentRequest request)
    {
        var analysisPrompt = $"""
        Perform a comprehensive security analysis of this code generation request:
        
        {request.Input}
        
        Check for potential vulnerabilities:
        1. SQL injection possibilities
        2. Cross-site scripting (XSS) risks
        3. Input validation weaknesses
        4. Authentication and authorization flaws
        5. Data exposure risks
        6. Cryptographic weaknesses
        7. Access control issues
        8. Configuration security problems
        9. Session management vulnerabilities
        10. API security issues
        
        Platform: {request.TargetPlatform}
        Security Requirements: {request.Parameters?.GetValueOrDefault("SecurityRequirements", "Standard")}
        
        Provide detailed security recommendations and secure coding alternatives.
        Rate the security level from 1-10 and identify specific vulnerabilities.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = analysisPrompt,
            Temperature = 0.2, // Low temperature for consistent security analysis
            MaxTokens = 1500
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to analyze security vulnerabilities");
            return new SecurityAnalysis
            {
                HasVulnerabilities = true,
                SecurityScore = 5.0,
                ComplianceLevel = "Unknown",
                Vulnerabilities = new[] { new SecurityVulnerability { Type = "AnalysisFailed", Severity = "High" } },
                Improvements = new[] { "Manual security review required" }
            };
        }
        
        return ParseSecurityAnalysis(response.Response);
    }

    /// <summary>
    /// Parses security analysis from model response
    /// </summary>
    private SecurityAnalysis ParseSecurityAnalysis(string response)
    {
        var vulnerabilities = new List<SecurityVulnerability>();
        var improvements = new List<string>();
        var securityScore = 7.0; // Default moderate score
        var complianceLevel = "Standard";
        
        // Parse vulnerabilities from response
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var lowerLine = line.ToLowerInvariant();
            
            // Identify vulnerability types
            if (lowerLine.Contains("sql injection"))
            {
                vulnerabilities.Add(new SecurityVulnerability
                {
                    Type = "SQLInjection",
                    Severity = "High",
                    Description = "Potential SQL injection vulnerability detected"
                });
            }
            
            if (lowerLine.Contains("xss") || lowerLine.Contains("cross-site scripting"))
            {
                vulnerabilities.Add(new SecurityVulnerability
                {
                    Type = "XSS",
                    Severity = "High",
                    Description = "Cross-site scripting vulnerability detected"
                });
            }
            
            if (lowerLine.Contains("authentication") || lowerLine.Contains("authorization"))
            {
                vulnerabilities.Add(new SecurityVulnerability
                {
                    Type = "Authentication",
                    Severity = "Medium",
                    Description = "Authentication/authorization issue detected"
                });
            }
            
            if (lowerLine.Contains("input validation"))
            {
                vulnerabilities.Add(new SecurityVulnerability
                {
                    Type = "InputValidation",
                    Severity = "Medium",
                    Description = "Input validation weakness detected"
                });
            }
            
            // Extract improvements
            if (line.Trim().StartsWith("-") || line.Trim().StartsWith("•"))
            {
                improvements.Add(line.Trim().TrimStart('-', '•').Trim());
            }
        }
        
        // Calculate security score based on vulnerabilities
        if (vulnerabilities.Any(v => v.Severity == "High"))
        {
            securityScore = 3.0;
            complianceLevel = "Low";
        }
        else if (vulnerabilities.Any(v => v.Severity == "Medium"))
        {
            securityScore = 6.0;
            complianceLevel = "Medium";
        }
        else if (!vulnerabilities.Any())
        {
            securityScore = 9.0;
            complianceLevel = "High";
        }
        
        return new SecurityAnalysis
        {
            HasVulnerabilities = vulnerabilities.Any(),
            SecurityScore = securityScore,
            ComplianceLevel = complianceLevel,
            Vulnerabilities = vulnerabilities.ToArray(),
            Improvements = improvements.Any() ? improvements.ToArray() : new[] { "Apply standard security practices" }
        };
    }
}
