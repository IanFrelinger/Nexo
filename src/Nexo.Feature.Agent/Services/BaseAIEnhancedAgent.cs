using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Base class for AI-enhanced agents, providing core functionality for AI processing, task analysis, and suggestion generation.
    /// </summary>
    public abstract class BaseAiEnhancedAgent : IAiEnhancedAgent
    {
        /// <summary>
        /// Provides logging functionality for classes that inherit from the BaseAiEnhancedAgent.
        /// Used to log relevant information, such as the processing of requests or lifecycle events,
        /// aiding in debugging, monitoring, and tracing operations within AI-enhanced agents.
        /// </summary>
        protected readonly ILogger<BaseAiEnhancedAgent> Logger;

        /// <summary>
        /// A protected, readonly instance of the <see cref="IModelOrchestrator"/> interface utilized for managing and executing
        /// AI model-related operations within the <see cref="BaseAiEnhancedAgent"/>.
        /// </summary>
        /// <remarks>
        /// This variable acts as the central component for interactions with AI models, providing functionalities such as
        /// executing model requests and handling responses. It is initialized via dependency injection through the constructor
        /// and is used extensively in methods that involve AI processing, such as task analysis, suggestion generation, and
        /// AI-enhanced operations.
        /// </remarks>
        protected readonly IModelOrchestrator _modelOrchestrator;

        /// <summary>
        /// Represents the base implementation for AI-enhanced agent functionality.
        /// </summary>
        /// <remarks>
        /// This abstract class serves as the foundation for all AI-enhanced agent types, providing core properties
        /// and shared functionality such as agent identity, name, role, status, and capabilities.
        /// Derived classes can extend its behavior as required for specific agent roles.
        /// </remarks>
        protected BaseAiEnhancedAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            ILogger<BaseAiEnhancedAgent> logger)
        {
            Id = id;
            Name = name;
            Role = role;
            Status = AgentStatus.Inactive;
            _modelOrchestrator = modelOrchestrator;
            Logger = logger;
            
            Capabilities = new List<string>();
            FocusAreas = new List<string>();
            AiCapabilities = new AiAgentCapabilities();
        }

        /// <summary>
        /// Gets the unique identifier of the AI-enhanced agent.
        /// Represents an instance of <see cref="AgentId"/>, which serves as the agent's primary identifier.
        /// </summary>
        public AgentId Id { get; }

        /// <summary>
        /// Gets the name of the AI-enhanced agent.
        /// </summary>
        /// <remarks>
        /// This property represents the unique, human-readable name of the agent.
        /// The name is typically used for identification and logging purposes.
        /// </remarks>
        public AgentName Name { get; }

        /// <summary>
        /// Represents the functional role assigned to an AI-enhanced agent within the system.
        /// </summary>
        /// <remarks>
        /// The <c>Role</c> property is used to define the specific responsibilities or designation
        /// of an agent. It helps in tailoring the agent's behavior and capabilities based on its
        /// assigned role.
        /// </remarks>
        public AgentRole Role { get; }

        /// <summary>
        /// Represents the current operational state of the agent.
        /// </summary>
        /// <remarks>
        /// The status can be one of the following values defined in the AgentStatus enumeration:
        /// Inactive, Active, Busy, or Failed. The status transitions are managed internally
        /// based on the agent's activities, such as processing requests or encountering errors.
        /// </remarks>
        public AgentStatus Status { get; protected set; }

        /// <summary>
        /// Gets the list of capabilities specific to the AI-enhanced agent.
        /// </summary>
        /// <remarks>
        /// This property represents a collection of strings that define the set of functionalities
        /// or specializations associated with the agent. Derived classes can initialize and populate
        /// this list with relevant capabilities based on their specific roles or purposes.
        /// </remarks>
        public List<string> Capabilities { get; }

        /// <summary>
        /// A collection representing the primary focus areas or domains of expertise
        /// associated with this agent. This property outlines the key architectural
        /// domains or patterns the agent specializes in, such as "Cloud-Native Architecture"
        /// or "Domain-Driven Design". It provides an overview of the agent's specialization
        /// for informed decision-making or task assignments.
        /// </summary>
        public List<string> FocusAreas { get; }

        /// Represents a service responsible for handling and orchestrating interactions
        /// with underlying AI models. Provides functionality to process and fulfill
        /// model-related tasks requested by various agents within the system.
        /// Typically injected into agents or other services requiring AI-enhanced capabilities.
        /// The interface serves as an abstraction layer to encapsulate the behavior
        /// of different AI models or orchestration implementations, enabling flexibility
        /// and ease of integration across the system.
        public IModelOrchestrator ModelOrchestrator => _modelOrchestrator;

        /// <summary>
        /// Gets the AI capabilities associated with the agent.
        /// </summary>
        /// <remarks>
        /// Provides configurable capabilities for the AI agent, such as code analysis, task analysis,
        /// problem-solving, and more. This property allows customization of AI functionality based on the
        /// specific role and focus areas of the agent.
        /// </remarks>
        public AiAgentCapabilities AiCapabilities { get; }

        /// <summary>
        /// Processes a given agent request asynchronously, updating the agent's status throughout the operation.
        /// </summary>
        /// <param name="request">The agent request to process.</param>
        /// <param name="ct">The cancellation token to observe for cancellation requests.</param>
        /// <returns>Returns the response generated from processing the agent request.</returns>
        public virtual async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            Logger.LogInformation("Processing request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            try
            {
                Status = AgentStatus.Busy;
                
                var response = await ProcessRequestInternalAsync(request, ct);
                
                Status = AgentStatus.Active;
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                throw;
            }
        }

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
                    response.AiModelUsed = aiResponse.Model;
                    response.Content = aiResponse.Content;
                    response.Success = true;
                    response.AiInsights = aiResponse.Metadata.ContainsKey("insights") 
                        ? (aiResponse.Metadata["insights"] as List<string>) ?? new List<string>()
                        : new List<string>();
                    response.AiConfidenceScore = aiResponse.Metadata.ContainsKey("confidence") 
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
                var request = new ModelRequest(0.9, 0.0, 0.0, false)
                {
                    Input = prompt,
                    MaxTokens = 1000,
                    Temperature = 0.3
                };

                var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
                return ParseTaskAnalysisResponse(response.Content);
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
        /// Generates AI-based suggestions for a given sprint task.
        /// </summary>
        /// <param name="task">The <see cref="SprintTask"/> object for which suggestions are to be generated.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="AiSuggestionsResult"/> containing the generated suggestions and related data.</returns>
        public virtual async Task<AiSuggestionsResult> GenerateSuggestionsAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Generating suggestions with AI for agent {AgentName}: {TaskId}", Name.Value, task.Id);

            try
            {
                var prompt = CreateSuggestionsPrompt(task);
                var request = new ModelRequest(0.9, 0.0, 0.0, false)
                {
                    Input = prompt,
                    MaxTokens = 1500,
                    Temperature = 0.4
                };

                var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
                return ParseSuggestionsResponse(response.Content);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating suggestions with AI for agent {AgentName}", Name.Value);
                return new AiSuggestionsResult
                {
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

        /// <summary>
        /// Starts the AI-enhanced agent and sets its status to active. Executes any additional start logic defined in the derived class.
        /// </summary>
        /// <param name="ct">A CancellationToken to observe while waiting for the operation's completion.</param>
        /// <returns>A task representing the asynchronous start operation.</returns>
        public virtual async Task StartAsync(CancellationToken ct)
        {
            Logger.LogInformation("Starting AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Active;
            await OnStartedAsync(ct);
        }

        /// <summary>
        /// Asynchronously stops the AI-enhanced agent and performs necessary cleanup operations.
        /// Updates the agent's status to inactive and triggers the OnStoppedAsync method for additional handling.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to signal the stop operation should be canceled.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous stop operation.</returns>
        public virtual async Task StopAsync(CancellationToken ct)
        {
            Logger.LogInformation("Stopping AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Inactive;
            await OnStoppedAsync(ct);
        }

        /// <summary>
        /// Processes an internal request asynchronously. This method is designed to be implemented by derived classes
        /// to handle the specifics of request processing based on the agent's functionality.
        /// </summary>
        /// <param name="request">The request to be processed, represented by an instance of <see cref="AgentRequest"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to observe cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an instance of <see cref="AgentResponse"/>
        /// which represents the outcome of processing the request.
        /// </returns>
        protected abstract Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken ct);

        /// <summary>
        /// Invoked when the AI-enhanced agent starts, allowing for any necessary initializations or task preparations.
        /// This method must be implemented by derived classes to define specific startup logic.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to observe cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected abstract Task OnStartedAsync(CancellationToken ct);

        /// <summary>
        /// Performs actions required when the AI-enhanced agent is stopped.
        /// This method is invoked during the stop lifecycle of the agent to handle cleanup or additional stopping logic.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> that propagates notification that the stop operation should be canceled.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected abstract Task OnStoppedAsync(CancellationToken ct);

        /// <summary>
        /// Processes an AI-enhanced request asynchronously using the specified AI model orchestrator.
        /// </summary>
        /// <param name="request">The AI-enhanced agent request containing the input for processing and associated metadata.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the AI model response.</returns>
        protected async Task<ModelResponse> ProcessWithAiAsync(AiEnhancedAgentRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreateProcessingPrompt(request);
            var modelRequest = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3,
                Metadata = request.AiContext
            };

            return await _modelOrchestrator.ExecuteAsync(modelRequest, cancellationToken);
        }

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

        /// <summary>
        /// Parses an AI-generated task analysis response and extracts the relevant analysis information.
        /// </summary>
        /// <param name="response">The raw string response from the AI model containing task analysis details.</param>
        /// <returns>An <see cref="AiTaskAnalysisResult"/> object containing the parsed analysis data such as summary, complexity assessment, and other insights.</returns>
        protected AiTaskAnalysisResult ParseTaskAnalysisResponse(string response)
        {
            try
            {
                // Simple JSON parsing - in production, use proper JSON deserialization
                var result = new AiTaskAnalysisResult();
                
                if (response.Contains("\"summary\""))
                {
                    var summaryMatch = System.Text.RegularExpressions.Regex.Match(response, "\"summary\":\\s*\"([^\"]+)\"");
                    if (summaryMatch.Success)
                        result.Summary = summaryMatch.Groups[1].Value;
                }

                if (response.Contains("\"complexityAssessment\""))
                {
                    var complexityMatch = System.Text.RegularExpressions.Regex.Match(response, "\"complexityAssessment\":\\s*\"([^\"]+)\"");
                    if (complexityMatch.Success)
                        result.ComplexityAssessment = complexityMatch.Groups[1].Value;
                }

                if (response.Contains("\"estimatedEffort\""))
                {
                    var effortMatch = System.Text.RegularExpressions.Regex.Match(response, "\"estimatedEffort\":\\s*\"([^\"]+)\"");
                    if (effortMatch.Success)
                        result.EstimatedEffort = effortMatch.Groups[1].Value;
                }

                if (response.Contains("\"recommendedApproach\""))
                {
                    var approachMatch = System.Text.RegularExpressions.Regex.Match(response, "\"recommendedApproach\":\\s*\"([^\"]+)\"");
                    if (approachMatch.Success)
                        result.RecommendedApproach = approachMatch.Groups[1].Value;
                }

                if (response.Contains("\"confidenceScore\""))
                {
                    var confidenceMatch = System.Text.RegularExpressions.Regex.Match(response, "\"confidenceScore\":\\s*([0-9.]+)");
                    if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
                        result.ConfidenceScore = confidence;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse AI task analysis response");
                return new AiTaskAnalysisResult
                {
                    Summary = "Failed to parse AI response",
                    ConfidenceScore = 0.0
                };
            }
        }

        /// <summary>
        /// Parses the response content received from the AI model to extract suggestion-related data into an <see cref="AiSuggestionsResult"/> object.
        /// </summary>
        /// <param name="response">The raw response string from the AI model containing suggestions and confidence score.</param>
        /// <returns>An <see cref="AiSuggestionsResult"/> object containing parsed improvement, code, architectural, and testing suggestions along with a confidence score.</returns>
        protected AiSuggestionsResult ParseSuggestionsResponse(string response)
        {
            try
            {
                var result = new AiSuggestionsResult();
                
                // Parse improvement suggestions
                if (response.Contains("\"improvementSuggestions\""))
                {
                    var improvementsMatch = System.Text.RegularExpressions.Regex.Match(response, "\"improvementSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (improvementsMatch.Success)
                    {
                        var suggestions = improvementsMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.ImprovementSuggestions = suggestions;
                    }
                }

                // Parse code suggestions
                if (response.Contains("\"codeSuggestions\""))
                {
                    var codeMatch = System.Text.RegularExpressions.Regex.Match(response, "\"codeSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (codeMatch.Success)
                    {
                        var suggestions = codeMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.CodeSuggestions = suggestions;
                    }
                }

                // Parse architectural suggestions
                if (response.Contains("\"architecturalSuggestions\""))
                {
                    var archMatch = System.Text.RegularExpressions.Regex.Match(response, "\"architecturalSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (archMatch.Success)
                    {
                        var suggestions = archMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.ArchitecturalSuggestions = suggestions;
                    }
                }

                // Parse testing suggestions
                if (response.Contains("\"testingSuggestions\""))
                {
                    var testMatch = System.Text.RegularExpressions.Regex.Match(response, "\"testingSuggestions\":\\s*\\[([^\\]]+)\\]");
                    if (testMatch.Success)
                    {
                        var suggestions = testMatch.Groups[1].Value.Split(',')
                            .Select(s => s.Trim().Trim('"'))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        result.TestingSuggestions = suggestions;
                    }
                }

                // Parse confidence score
                if (!response.Contains("\"confidenceScore\"")) return result;
                var confidenceMatch = System.Text.RegularExpressions.Regex.Match(response, "\"confidenceScore\":\\s*([0-9.]+)");
                if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
                    result.ConfidenceScore = confidence;

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse AI suggestions response");
                return new AiSuggestionsResult
                {
                    ConfidenceScore = 0.0
                };
            }
        }

        /// <summary>
        /// Extracts keywords from the given text. Keywords are identified as distinct words
        /// with a length greater than 3, converted to lowercase, and stripped of certain special characters.
        /// </summary>
        /// <param name="text">The input text from which keywords will be extracted.</param>
        /// <returns>A list of distinct, lowercase keywords derived from the input text.</returns>
        protected List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3)
                .Select(word => word.ToLowerInvariant())
                .Distinct()
                .ToList();
        }
    }
} 