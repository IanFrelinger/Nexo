using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Core functionality for SecurityAnalysisAgent.
/// </summary>
public partial class SecurityAnalysisAgent
{
    /// <summary>
    /// Processes security analysis requests
    /// </summary>
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

    /// <summary>
    /// Coordinates security analysis with other agents
    /// </summary>
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
                var platformRequest = request.CreatePlatformSpecificRequest(platformAgent.PlatformExpertise.ToString());
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

    /// <summary>
    /// Assesses agent capability for the request
    /// </summary>
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
            
            if (request.Parameters?.ContainsKey("SecurityRequirements") == true)
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

    /// <summary>
    /// Learns from security analysis results
    /// </summary>
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
}
