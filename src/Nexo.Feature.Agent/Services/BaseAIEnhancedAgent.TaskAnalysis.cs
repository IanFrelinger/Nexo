using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI task analysis functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Analyzes the provided sprint task using AI mechanisms to generate an evaluation or insight about the task.
        /// This method interacts with AI model orchestrators to process the task information, analyze it, and
        /// return a result that includes a summary and a confidence score based on AI processing.
        /// The AI analysis is performed by forming a prompt containing task data, sending it to the AI model,
        /// and processing the response to extract the required information.
        /// In case of an error during the AI analysis process, it logs the details of the exception and returns
        /// an analysis result indicating an error occurred.
        /// </summary>
        /// <param name="task">
        /// The sprint task to be analyzed using AI technology. This object contains task-specific information
        /// required for AI evaluation.
        /// </param>
        /// <param name="cancellationToken">
        /// Optional cancellation token to cancel the AI analysis operation if required.
        /// </param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The task result contains an instance of
        /// <see cref="AiTaskAnalysisResult"/>, which holds the analysis details including a summary
        /// and confidence score.
        /// </returns>
        public virtual async Task<AiTaskAnalysisResult> AnalyzeTaskWithAiAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Analyzing task with AI for agent {AgentName}: {TaskId}", Name.Value, task.Id);

            try
            {
                var prompt = CreateTaskAnalysisPrompt(task);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 1000,
                    Temperature = 0.3
                };

                var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
                return ParseTaskAnalysisResponse(response.Response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing task with AI for agent {AgentName}", Name.Value);
                return new AiTaskAnalysisResult
                {
                    Summary = "Error occurred during AI analysis",
                    ConfidenceScore = 0.0
                };
            }
        }

        /// <summary>
        /// Determines whether the agent can handle the given task based on focus areas and AI task analysis.
        /// </summary>
        /// <param name="task">The sprint task to be evaluated.</param>
        /// <param name="ct">The cancellation token used to observe cancellation requests.</param>
        /// <returns>
        /// A boolean value indicating whether the agent can handle the task.
        /// Returns true if the task aligns with the agent's focus areas or if AI analysis determines high confidence; otherwise, false.
        /// </returns>
        public virtual async Task<bool> CanHandleTaskAsync(SprintTask task, CancellationToken ct)
        {
            // Check if any focus areas match the task
            var taskKeywords = ExtractKeywords(task.Description);
            var hasMatchingFocus = FocusAreas.Any(focus => 
                taskKeywords.Any(keyword => focus.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));

            if (hasMatchingFocus)
            {
                return true;
            }

            // Use AI to analyze task if capabilities allow
            if (AiCapabilities.CanAnalyzeTasks)
            {
                try
                {
                    var analysis = await AnalyzeTaskWithAiAsync(task, ct);
                    return analysis.ConfidenceScore > 0.6; // Threshold for AI confidence
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "AI task analysis failed for agent {AgentName}, falling back to basic matching", Name.Value);
                }
            }

            return false;
        }
    }
}
