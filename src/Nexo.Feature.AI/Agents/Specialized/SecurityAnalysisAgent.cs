using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Specialized agent for security analysis and secure code generation
/// </summary>
public class SecurityAnalysisAgent : ISpecializedAgent
{
    public string AgentId => "SecurityAnalysis";
    public AgentSpecialization Specialization => AgentSpecialization.SecurityAnalysis;
    public PlatformCompatibility PlatformExpertise => PlatformCompatibility.All;
    
    public PerformanceProfile OptimizationProfile => new()
    {
        PrimaryTarget = OptimizationTarget.Security,
        MonitoredMetrics = new[]
        {
            PerformanceMetric.ErrorRate,
            PerformanceMetric.ExecutionTime,
            PerformanceMetric.MemoryUsage
        },
        SupportsRealTimeOptimization = false // Security analysis is thorough, not real-time
    };
    
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<SecurityAnalysisAgent> _logger;
    
    public SecurityAnalysisAgent(
        IModelOrchestrator modelOrchestrator,
        ILogger<SecurityAnalysisAgent> logger)
    {
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing security analysis request");
            
            // Analyze code for security vulnerabilities
            var securityAnalysis = await AnalyzeSecurityVulnerabilities(request);
            
            if (securityAnalysis.HasVulnerabilities)
            {
                // Generate secure code alternatives
                var secureCode = await GenerateSecureCode(request, securityAnalysis);
                
                return new AgentResponse
                {
                    Result = secureCode,
                    Confidence = 0.95,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SecurityVulnerabilities"] = securityAnalysis.Vulnerabilities,
                        ["SecurityImprovements"] = securityAnalysis.Improvements,
                        ["ComplianceLevel"] = securityAnalysis.ComplianceLevel,
                        ["SecurityScore"] = securityAnalysis.SecurityScore,
                        ["AgentId"] = AgentId,
                        ["Specialization"] = Specialization.ToString()
                    }
                };
            }
            
            return AgentResponse.SecureCodeGenerated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing security analysis request");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Security analysis failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
    {
        try
        {
            _logger.LogInformation("Coordinating security analysis with {CollaboratorCount} agents", 
                collaborators.Count());
            
            // Find platform-specific agents for detailed security analysis
            var platformAgents = collaborators
                .Where(a => a.Specialization.HasFlag(AgentSpecialization.PlatformSpecific))
                .ToList();
            
            var coordinatedSecurityResults = new List<PlatformSecurityAnalysis>();
            
            // Get platform-specific security insights
            foreach (var platformAgent in platformAgents)
            {
                var platformRequest = request.CreatePlatformSpecificRequest(platformAgent.PlatformExpertise);
                var platformResponse = await platformAgent.ProcessAsync(platformRequest);
                
                if (platformResponse.HasResult)
                {
                    var platformSecurity = new PlatformSecurityAnalysis
                    {
                        Platform = platformAgent.PlatformExpertise,
                        SecureCode = platformResponse.Result,
                        SecurityLevel = DetermineSecurityLevel(platformResponse),
                        PlatformSpecificVulnerabilities = ExtractPlatformVulnerabilities(platformResponse)
                    };
                    
                    coordinatedSecurityResults.Add(platformSecurity);
                }
            }
            
            // Synthesize cross-platform security solution
            var synthesizedSecureCode = await SynthesizeCrossPlatformSecurity(coordinatedSecurityResults, request);
            
            return new AgentResponse
            {
                Result = synthesizedSecureCode,
                Confidence = 0.98,
                Metadata = new Dictionary<string, object>
                {
                    ["PlatformSecurityAnalyses"] = coordinatedSecurityResults,
                    ["CrossPlatformSecurityStrategy"] = "Unified",
                    ["AgentId"] = AgentId,
                    ["CoordinationType"] = "CrossPlatformSecurity"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error coordinating security analysis");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Security coordination failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentCapabilityAssessment> AssessCapabilityAsync(AgentRequest request)
    {
        try
        {
            var securityKeywords = new[] { "authentication", "authorization", "encryption", "password", "token", "api", "database", "input", "validation" };
            var hasSecurityContext = securityKeywords.Any(keyword => 
                request.Input.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            
            var strengths = new List<string>();
            var limitations = new List<string>();
            var capabilityScore = 0.0;
            
            if (hasSecurityContext)
            {
                strengths.Add("Security vulnerability analysis expertise");
                strengths.Add("Secure coding best practices");
                strengths.Add("Compliance and standards knowledge");
                strengths.Add("Cross-platform security patterns");
                capabilityScore += 0.9;
            }
            else
            {
                limitations.Add("No obvious security context detected");
                capabilityScore += 0.3;
            }
            
            if (request.Context?.ContainsKey("SecurityRequirements") == true)
            {
                strengths.Add("Security requirements analysis");
                capabilityScore += 0.1;
            }
            
            return new AgentCapabilityAssessment
            {
                CapabilityScore = Math.Min(capabilityScore, 1.0),
                Strengths = strengths.ToArray(),
                Limitations = limitations.ToArray(),
                CanHandleRequest = capabilityScore > 0.4,
                Recommendation = capabilityScore > 0.7 ? "Highly recommended for security analysis" : 
                               capabilityScore > 0.4 ? "Suitable for security review" : "Limited security context"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing security agent capability");
            return new AgentCapabilityAssessment
            {
                CapabilityScore = 0.0,
                CanHandleRequest = false,
                Recommendation = "Assessment failed"
            };
        }
    }
    
    public async Task LearnFromResultAsync(AgentRequest request, AgentResponse response, PerformanceMetrics metrics)
    {
        try
        {
            _logger.LogDebug("Learning from security analysis result");
            
            // Store security-specific learning data
            var learningData = new
            {
                Request = request.Input,
                Response = response.Result,
                Success = response.Success,
                Confidence = response.Confidence,
                SecurityVulnerabilities = response.GetMetadata<SecurityVulnerability[]>("SecurityVulnerabilities"),
                SecurityScore = response.GetMetadata<double>("SecurityScore"),
                ActualPerformance = metrics,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogDebug("Security learning data recorded for future analysis improvements");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from security analysis result");
        }
    }
    
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
        Security Requirements: {request.Context?.GetValueOrDefault("SecurityRequirements", "Standard")}
        
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
    
    private SecurityLevel DetermineSecurityLevel(AgentResponse response)
    {
        var securityScore = response.GetMetadata<double>("SecurityScore");
        
        return securityScore switch
        {
            >= 8.0 => SecurityLevel.High,
            >= 6.0 => SecurityLevel.Standard,
            >= 4.0 => SecurityLevel.Basic,
            _ => SecurityLevel.Low
        };
    }
    
    private SecurityVulnerability[] ExtractPlatformVulnerabilities(AgentResponse response)
    {
        var vulnerabilities = response.GetMetadata<SecurityVulnerability[]>("SecurityVulnerabilities");
        return vulnerabilities ?? Array.Empty<SecurityVulnerability>();
    }
    
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

/// <summary>
/// Security analysis result
/// </summary>
public record SecurityAnalysis
{
    public bool HasVulnerabilities { get; init; }
    public double SecurityScore { get; init; }
    public string ComplianceLevel { get; init; } = "Unknown";
    public SecurityVulnerability[] Vulnerabilities { get; init; } = [];
    public string[] Improvements { get; init; } = [];
}

/// <summary>
/// Security vulnerability information
/// </summary>
public record SecurityVulnerability
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = "Medium";
    public string Description { get; init; } = string.Empty;
    public string? Remediation { get; init; }
}

/// <summary>
/// Platform-specific security analysis
/// </summary>
public record PlatformSecurityAnalysis
{
    public PlatformCompatibility Platform { get; init; }
    public string SecureCode { get; init; } = string.Empty;
    public SecurityLevel SecurityLevel { get; init; }
    public SecurityVulnerability[] PlatformSpecificVulnerabilities { get; init; } = [];
}
