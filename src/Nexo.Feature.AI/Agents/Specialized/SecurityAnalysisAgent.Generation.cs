using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Secure code generation functionality for SecurityAnalysisAgent.
/// </summary>
public partial class SecurityAnalysisAgent
{
    /// <summary>
    /// Generates secure code based on security analysis
    /// </summary>
    private async Task<string> GenerateSecureCode(AgentRequest request, SecurityAnalysis analysis)
    {
        var secureCodePrompt = $"""
        Generate secure code for this request, addressing the identified vulnerabilities:
        
        Original Request: {request.Input}
        
        Identified Vulnerabilities:
        """;
        
        foreach (var vuln in analysis.Vulnerabilities)
        {
            secureCodePrompt += $"- {vuln.Type} ({vuln.Severity}): {vuln.Description}\n";
        }
        
        secureCodePrompt += $"""
        
        Security Improvements Needed:
        """;
        
        foreach (var improvement in analysis.Improvements)
        {
            secureCodePrompt += $"- {improvement}\n";
        }
        
        secureCodePrompt += """
        
        Generate secure code that:
        1. Addresses all identified vulnerabilities
        2. Implements proper input validation and sanitization
        3. Uses secure authentication and authorization patterns
        4. Implements proper error handling without information leakage
        5. Uses parameterized queries and prepared statements
        6. Implements proper session management
        7. Uses secure communication protocols
        8. Implements proper logging and monitoring
        9. Follows security best practices and standards
        10. Includes security comments and documentation
        
        Provide the complete, secure implementation.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = secureCodePrompt,
            Temperature = 0.3,
            MaxTokens = 2000
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogError("Failed to generate secure code");
            return request.Input; // Return original if secure generation fails
        }
        
        return response.Response;
    }

    /// <summary>
    /// Synthesizes cross-platform security solutions
    /// </summary>
    private async Task<string> SynthesizeCrossPlatformSecurity(
        IEnumerable<PlatformSecurityAnalysis> platformAnalyses, 
        AgentRequest request)
    {
        var synthesisPrompt = $"""
        Synthesize these platform-specific security analyses into a unified, secure solution:
        
        Original Request: {request.Input}
        
        Platform Security Analyses:
        """;
        
        foreach (var analysis in platformAnalyses)
        {
            synthesisPrompt += $"\n{analysis.Platform} (Security Level: {analysis.SecurityLevel}):\n{analysis.SecureCode}\n";
            
            if (analysis.PlatformSpecificVulnerabilities.Any())
            {
                synthesisPrompt += "Platform-specific vulnerabilities:\n";
                foreach (var vuln in analysis.PlatformSpecificVulnerabilities)
                {
                    synthesisPrompt += $"- {vuln.Type}: {vuln.Description}\n";
                }
            }
        }
        
        synthesisPrompt += """
        
        Create a unified security solution that:
        1. Combines the best security practices from all platforms
        2. Addresses platform-specific vulnerabilities
        3. Maintains consistent security standards across platforms
        4. Implements defense in depth
        5. Includes comprehensive security monitoring
        6. Provides clear security documentation
        7. Handles platform differences securely
        8. Implements proper security testing
        
        Generate the final, unified secure code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = synthesisPrompt,
            Temperature = 0.2,
            MaxTokens = 2500
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogError("Failed to synthesize cross-platform security");
            return request.Input;
        }
        
        return response.Response;
    }
}
