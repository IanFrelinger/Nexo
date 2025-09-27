using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// Core functionality for Unity optimization agent
    /// </summary>
    public partial class UnityOptimizationAgent
    {
        public async Task<AgentResponse> ProcessAsync(AgentRequest request)
        {
            _logger.LogInformation("Processing Unity optimization request");
            
            try
            {
                var optimizationRequest = request.GetUnityOptimizationRequest();
                
                // Analyze Unity project for optimization opportunities
                var projectAnalysis = await _projectAnalyzer.AnalyzeProjectAsync(optimizationRequest.ProjectPath);
                
                // Generate AI-powered optimization recommendations
                var optimizationRecommendations = await GenerateOptimizationRecommendations(projectAnalysis, optimizationRequest);
                
                // Create implementation plan
                var implementationPlan = await CreateImplementationPlan(optimizationRecommendations, projectAnalysis);
                
                return new AgentResponse
                {
                    Result = implementationPlan,
                    Confidence = 0.9,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ProjectAnalysis"] = projectAnalysis,
                        ["OptimizationRecommendations"] = optimizationRecommendations,
                        ["EstimatedImprovements"] = CalculateEstimatedImprovements(optimizationRecommendations)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Unity optimization request");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }

        public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
        {
            _logger.LogInformation("Coordinating Unity optimization with other agents");
            
            try
            {
                // Coordinate with gameplay balance agent for performance-balanced mechanics
                var balanceAgent = collaborators.FirstOrDefault(a => a.AgentId == "GameplayBalance");
                
                if (balanceAgent != null)
                {
                    // Get balance implications of performance optimizations
                    var balanceRequest = request.CreateBalanceImplicationRequest();
                    var balanceResponse = await balanceAgent.ProcessAsync(balanceRequest);
                    
                    // Generate optimizations with balance considerations
                    var optimizationResponse = await ProcessAsync(request);
                    
                    // Integrate balance feedback
                    var integratedOptimizations = await IntegrateBalanceConsiderations(optimizationResponse, balanceResponse);
                    
                    return new AgentResponse
                    {
                        Result = integratedOptimizations,
                        Confidence = Math.Min(optimizationResponse.Confidence, balanceResponse.Confidence),
                        Metadata = MergeMetadata(optimizationResponse.Metadata, balanceResponse.Metadata)
                    };
                }
                
                return await ProcessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to coordinate Unity optimization");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }
    }
}
