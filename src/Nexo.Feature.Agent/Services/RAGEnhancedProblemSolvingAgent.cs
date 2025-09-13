using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// RAG-enhanced problem-solving agent that provides intelligent problem analysis and solutions.
    /// </summary>
    public class RAGEnhancedProblemSolvingAgent : BaseAiEnhancedAgent
    {
        public RAGEnhancedProblemSolvingAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            ILogger<BaseAiEnhancedAgent> logger)
            : base(id, name, role, modelOrchestrator, logger)
        {
            // Initialize capabilities
            AiCapabilities.CanAnalyzeCode = true;
            AiCapabilities.CanGenerateCode = true;
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanSolveProblems = true;
            
            // Set focus areas
            FocusAreas.Add("Problem Analysis");
            FocusAreas.Add("Solution Design");
            FocusAreas.Add("Root Cause Analysis");
            FocusAreas.Add("Collaborative Problem Solving");
            
            // Set capabilities
            Capabilities.Add("Problem Analysis");
            Capabilities.Add("Solution Design");
            Capabilities.Add("Root Cause Analysis");
            Capabilities.Add("Collaboration");
            Capabilities.Add("Risk Assessment");
        }

        protected override async Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Processing problem-solving agent request: {Type}", request.Type);

                var response = new AgentResponse
                {
                    Success = false,
                    Content = string.Empty
                };

                // Process based on request type
                switch (request.Type)
                {
                    case AgentRequestType.Analysis:
                        response = await ProcessProblemAnalysisAsync(request, cancellationToken);
                        break;
                    case AgentRequestType.BugFix:
                        response = await ProcessProblemResolutionAsync(request, cancellationToken);
                        break;
                    case AgentRequestType.Collaboration:
                        response = await ProcessCollaborativeProblemSolvingAsync(request, cancellationToken);
                        break;
                    default:
                        response.Content = "Unsupported request type for problem-solving agent";
                        break;
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing problem-solving agent request");
                return new AgentResponse
                {
                    Success = false,
                    Content = $"Error processing request: {ex.Message}"
                };
            }
        }

        private async Task<AgentResponse> ProcessProblemAnalysisAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var problemDescription = request.Context.ContainsKey("problem") ? request.Context["problem"]?.ToString() ?? string.Empty : request.Content;
                
                if (string.IsNullOrEmpty(problemDescription))
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Content = "No problem description provided for analysis"
                    };
                }

                var analysisResult = $"Problem Analysis:\n\n";
                analysisResult += $"Problem: {problemDescription}\n\n";
                
                analysisResult += "Problem Breakdown:\n";
                analysisResult += "1. Identify root cause\n";
                analysisResult += "2. Analyze contributing factors\n";
                analysisResult += "3. Evaluate impact and severity\n";
                analysisResult += "4. Consider constraints and limitations\n\n";

                analysisResult += "Recommended Approach:\n";
                analysisResult += "- Gather more information about the problem context\n";
                analysisResult += "- Test potential solutions in isolation\n";
                analysisResult += "- Consider alternative approaches\n";
                analysisResult += "- Document findings and solutions\n";

                return new AgentResponse
                {
                    Success = true,
                    Content = analysisResult
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing problem analysis");
                return new AgentResponse
                {
                    Success = false,
                    Content = $"Error during problem analysis: {ex.Message}"
                };
            }
        }

        private async Task<AgentResponse> ProcessProblemResolutionAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var problemDetails = request.Context.ContainsKey("problem") ? request.Context["problem"]?.ToString() ?? string.Empty : request.Content;
                var errorMessage = request.Context.ContainsKey("error") ? request.Context["error"]?.ToString() ?? string.Empty : string.Empty;
                
                if (string.IsNullOrEmpty(problemDetails))
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Content = "No problem details provided for resolution"
                    };
                }

                var resolutionResult = $"Problem Resolution Plan:\n\n";
                resolutionResult += $"Problem: {problemDetails}\n";
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    resolutionResult += $"Error: {errorMessage}\n";
                }
                resolutionResult += "\n";

                resolutionResult += "Resolution Steps:\n";
                resolutionResult += "1. Isolate the problem\n";
                resolutionResult += "2. Identify the root cause\n";
                resolutionResult += "3. Develop a solution strategy\n";
                resolutionResult += "4. Implement the fix\n";
                resolutionResult += "5. Test the solution\n";
                resolutionResult += "6. Verify the fix works\n\n";

                resolutionResult += "Prevention Strategies:\n";
                resolutionResult += "- Add input validation\n";
                resolutionResult += "- Improve error handling\n";
                resolutionResult += "- Add logging for debugging\n";
                resolutionResult += "- Write comprehensive tests\n";

                return new AgentResponse
                {
                    Success = true,
                    Content = resolutionResult
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing problem resolution");
                return new AgentResponse
                {
                    Success = false,
                    Content = $"Error during problem resolution: {ex.Message}"
                };
            }
        }

        private async Task<AgentResponse> ProcessCollaborativeProblemSolvingAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var collaborationContext = request.Context.ContainsKey("collaboration") ? request.Context["collaboration"]?.ToString() ?? string.Empty : request.Content;
                
                if (string.IsNullOrEmpty(collaborationContext))
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Content = "No collaboration context provided"
                    };
                }

                var collaborationResult = $"Collaborative Problem Solving:\n\n";
                collaborationResult += $"Context: {collaborationContext}\n\n";
                
                collaborationResult += "Collaboration Strategy:\n";
                collaborationResult += "1. Define clear roles and responsibilities\n";
                collaborationResult += "2. Establish communication channels\n";
                collaborationResult += "3. Set up regular check-ins\n";
                collaborationResult += "4. Create shared documentation\n";
                collaborationResult += "5. Implement version control practices\n\n";

                collaborationResult += "Best Practices:\n";
                collaborationResult += "- Use pair programming for complex problems\n";
                collaborationResult += "- Conduct regular code reviews\n";
                collaborationResult += "- Maintain clear documentation\n";
                collaborationResult += "- Share knowledge and lessons learned\n";

                return new AgentResponse
                {
                    Success = true,
                    Content = collaborationResult
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing collaborative problem solving");
                return new AgentResponse
                {
                    Success = false,
                    Content = $"Error during collaborative problem solving: {ex.Message}"
                };
            }
        }

        protected override async Task OnStartedAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("RAG-enhanced problem-solving agent started");
            await Task.CompletedTask;
        }

        protected override async Task OnStoppedAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("RAG-enhanced problem-solving agent stopped");
            await Task.CompletedTask;
        }
    }
}