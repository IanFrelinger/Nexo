using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI-enhanced request processing functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Processes an AI-enhanced request asynchronously and returns a response containing the results of the AI processing.
        /// </summary>
        /// <param name="request">The AI-enhanced request containing the details and parameters for the process.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation before completion.</param>
        /// <returns>A task that represents the asynchronous operation, containing the AI-enhanced response with the processing results.</returns>
        public virtual async Task<AiEnhancedAgentResponse> ProcessAiRequestAsync(AiEnhancedAgentRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Processing AI request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            var startTime = DateTime.UtcNow;
            var response = new AiEnhancedAgentResponse();

            try
            {
                Status = AgentStatus.Busy;

                if (request.UseAi && AiCapabilities.CanAnalyzeTasks)
                {
                    // Process with AI enhancement
                    var aiResponse = await ProcessWithAiAsync(request, cancellationToken);
                    response.AiWasUsed = true;
                    response.AiModelUsed = "AI Model"; // Model name not available in new API
                    response.Content = aiResponse.Response;
                    response.Success = true;
                    response.AiInsights = aiResponse.Metadata?.ContainsKey("insights") == true 
                        ? (aiResponse.Metadata["insights"] as List<string>) ?? new List<string>()
                        : new List<string>();
                    response.AiConfidenceScore = aiResponse.Metadata?.ContainsKey("confidence") == true 
                        ? Convert.ToDouble(aiResponse.Metadata["confidence"]) 
                        : 0.0;
                }
                else
                {
                    // Fall back to standard processing
                    var standardResponse = await ProcessRequestAsync(request, cancellationToken);
                    response = new AiEnhancedAgentResponse
                    {
                        Content = standardResponse.Content,
                        Success = standardResponse.Success,
                        AiWasUsed = false
                    };
                }

                response.AiProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                Status = AgentStatus.Active;
                
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing AI request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                return new AiEnhancedAgentResponse
                {
                    Success = false,
                    Content = $"Error processing request: {ex.Message}",
                    AiWasUsed = false
                };
            }
        }

        /// <summary>
        /// Processes an AI-enhanced request asynchronously using the specified AI model orchestrator.
        /// </summary>
        /// <param name="request">The AI-enhanced agent request containing the input for processing and associated metadata.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the AI model response.</returns>
        protected async Task<ModelResponse> ProcessWithAiAsync(AiEnhancedAgentRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreateProcessingPrompt(request);
            var modelRequest = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            return await _modelOrchestrator.ExecuteAsync(modelRequest, cancellationToken);
        }
    }
}
