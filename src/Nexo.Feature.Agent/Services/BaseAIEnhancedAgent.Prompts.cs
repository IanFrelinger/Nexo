using System;
using System.Linq;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI prompt creation functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Constructs a processing prompt based on the specified AI-enhanced agent request,
        /// including context about the agent's attributes and the request details.
        /// </summary>
        /// <param name="request">The AI-enhanced agent request containing the type and content to process.</param>
        /// <returns>A formatted string representing the detailed processing prompt.</returns>
        protected virtual string CreateProcessingPrompt(AiEnhancedAgentRequest request)
        {
            return $@"You are an AI-enhanced agent with the following characteristics:
- Name: {Name.Value}
- Role: {Role.Value}
- Capabilities: {string.Join(", ", Capabilities)}
- Focus Areas: {string.Join(", ", FocusAreas)}

Request Type: {request.Type}
Request Content: {request.Content}

Please provide a comprehensive response that leverages your AI capabilities to enhance the processing of this request. Consider the context and provide insights, suggestions, or improvements where applicable.";
        }

        /// <summary>
        /// Creates a formatted task analysis prompt for an AI-enhanced agent using task details and agent information.
        /// The prompt is intended for generating a structured analysis of a task, including summary, complexity assessment,
        /// estimated effort, recommended approach, potential risks, and confidence score in JSON format.
        /// </summary>
        /// <param name="task">The sprint task to be analyzed, containing details like ID, description, priority, and story points.</param>
        /// <returns>A string containing the formatted task analysis prompt to be used by the AI model.</returns>
        protected string CreateTaskAnalysisPrompt(SprintTask task)
        {
            return $@"Analyze the following task for an AI-enhanced agent:

Agent Information:
- Name: {Name.Value}
- Role: {Role.Value}
- Capabilities: {string.Join(", ", Capabilities)}
- Focus Areas: {string.Join(", ", FocusAreas)}

Task Information:
- ID: {task.Id}
- Description: {task.Description}
- Priority: {task.Priority}
- Story Points: {task.StoryPoints}

Please provide a structured analysis including:
1. Summary of the task
2. Complexity assessment
3. Estimated effort
4. Recommended approach
5. Potential risks
6. Confidence score (0.0 to 1.0)

Format your response as JSON with these fields: summary, complexityAssessment, estimatedEffort, recommendedApproach, potentialRisks, confidenceScore.";
        }

        /// <summary>
        /// Generates a prompt string for creating suggestions based on the given task.
        /// The generated prompt includes information about the agent and the task, and
        /// specifies the format for the suggestions to be provided in JSON.
        /// </summary>
        /// <param name="task">The task for which suggestions need to be generated. This includes details such as the description and priority.</param>
        /// <returns>A formatted prompt string containing agent and task information, along with instructions for generating suggestions.</returns>
        protected virtual string CreateSuggestionsPrompt(SprintTask task)
        {
            return $@"Generate suggestions for the following task:

Agent Information:
- Name: {Name.Value}
- Role: {Role.Value}
- Capabilities: {string.Join(", ", Capabilities)}

Task Information:
- Description: {task.Description}
- Priority: {task.Priority}

Please provide suggestions in the following categories:
1. Improvement suggestions
2. Code suggestions
3. Architectural suggestions
4. Testing suggestions

Format your response as JSON with these fields: improvementSuggestions, codeSuggestions, architecturalSuggestions, testingSuggestions, confidenceScore.";
        }
    }
}
