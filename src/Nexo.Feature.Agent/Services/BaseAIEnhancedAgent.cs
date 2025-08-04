using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Base implementation for AI-enhanced agents providing common AI processing capabilities.
    /// </summary>
    public abstract class BaseAIEnhancedAgent : IAIEnhancedAgent
    {
        protected readonly ILogger<BaseAIEnhancedAgent> _logger;
        protected readonly IModelOrchestrator _modelOrchestrator;

        protected BaseAIEnhancedAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            ILogger<BaseAIEnhancedAgent> logger)
        {
            Id = id;
            Name = name;
            Role = role;
            Status = AgentStatus.Inactive;
            _modelOrchestrator = modelOrchestrator;
            _logger = logger;
            
            Capabilities = new List<string>();
            FocusAreas = new List<string>();
            AICapabilities = new AIAgentCapabilities();
        }

        public AgentId Id { get; }
        public AgentName Name { get; }
        public AgentRole Role { get; }
        public AgentStatus Status { get; protected set; }
        public List<string> Capabilities { get; }
        public List<string> FocusAreas { get; }
        public IModelOrchestrator ModelOrchestrator => _modelOrchestrator;
        public AIAgentCapabilities AICapabilities { get; }

        public virtual async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Processing request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            try
            {
                Status = AgentStatus.Busy;
                
                var response = await ProcessRequestInternalAsync(request, ct);
                
                Status = AgentStatus.Active;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                throw;
            }
        }

        public virtual async Task<AIEnhancedAgentResponse> ProcessAIRequestAsync(AIEnhancedAgentRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Processing AI request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            var startTime = DateTime.UtcNow;
            var response = new AIEnhancedAgentResponse();

            try
            {
                Status = AgentStatus.Busy;

                if (request.UseAI && AICapabilities.CanAnalyzeTasks)
                {
                    // Process with AI enhancement
                    var aiResponse = await ProcessWithAIAsync(request, cancellationToken);
                    response.AIWasUsed = true;
                    response.AIModelUsed = aiResponse.Model;
                    response.Content = aiResponse.Content;
                    response.Success = true;
                    response.AIInsights = aiResponse.Metadata.ContainsKey("insights") 
                        ? (aiResponse.Metadata["insights"] as List<string>) ?? new List<string>()
                        : new List<string>();
                    response.AIConfidenceScore = aiResponse.Metadata.ContainsKey("confidence") 
                        ? Convert.ToDouble(aiResponse.Metadata["confidence"]) 
                        : 0.0;
                }
                else
                {
                    // Fall back to standard processing
                    var standardResponse = await ProcessRequestAsync(request, cancellationToken);
                    response = new AIEnhancedAgentResponse
                    {
                        Content = standardResponse.Content,
                        Success = standardResponse.Success,
                        AIWasUsed = false
                    };
                }

                response.AIProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                Status = AgentStatus.Active;
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                return new AIEnhancedAgentResponse
                {
                    Success = false,
                    Content = $"Error processing request: {ex.Message}",
                    AIWasUsed = false
                };
            }
        }

        public virtual async Task<AITaskAnalysisResult> AnalyzeTaskWithAIAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Analyzing task with AI for agent {AgentName}: {TaskId}", Name.Value, task.Id);

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
                return ParseTaskAnalysisResponse(response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing task with AI for agent {AgentName}", Name.Value);
                return new AITaskAnalysisResult
                {
                    Summary = "Error occurred during AI analysis",
                    ConfidenceScore = 0.0
                };
            }
        }

        public virtual async Task<AISuggestionsResult> GenerateSuggestionsAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Generating suggestions with AI for agent {AgentName}: {TaskId}", Name.Value, task.Id);

            try
            {
                var prompt = CreateSuggestionsPrompt(task);
                var request = new ModelRequest
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
                _logger.LogError(ex, "Error generating suggestions with AI for agent {AgentName}", Name.Value);
                return new AISuggestionsResult
                {
                    ConfidenceScore = 0.0
                };
            }
        }

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
            if (AICapabilities.CanAnalyzeTasks)
            {
                try
                {
                    var analysis = await AnalyzeTaskWithAIAsync(task, ct);
                    return analysis.ConfidenceScore > 0.6; // Threshold for AI confidence
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI task analysis failed for agent {AgentName}, falling back to basic matching", Name.Value);
                }
            }

            return false;
        }

        public virtual async Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Active;
            await OnStartedAsync(ct);
        }

        public virtual async Task StopAsync(CancellationToken ct)
        {
            _logger.LogInformation("Stopping AI-enhanced agent {AgentName}", Name.Value);
            Status = AgentStatus.Inactive;
            await OnStoppedAsync(ct);
        }

        protected abstract Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken ct);
        protected abstract Task OnStartedAsync(CancellationToken ct);
        protected abstract Task OnStoppedAsync(CancellationToken ct);

        protected virtual async Task<Nexo.Feature.AI.Models.ModelResponse> ProcessWithAIAsync(AIEnhancedAgentRequest request, CancellationToken cancellationToken)
        {
            var prompt = CreateProcessingPrompt(request);
            var modelRequest = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3,
                Metadata = request.AIContext
            };

            return await _modelOrchestrator.ExecuteAsync(modelRequest, cancellationToken);
        }

        protected virtual string CreateProcessingPrompt(AIEnhancedAgentRequest request)
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

        protected virtual string CreateTaskAnalysisPrompt(SprintTask task)
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

        protected virtual AITaskAnalysisResult ParseTaskAnalysisResponse(string response)
        {
            try
            {
                // Simple JSON parsing - in production, use proper JSON deserialization
                var result = new AITaskAnalysisResult();
                
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
                _logger.LogWarning(ex, "Failed to parse AI task analysis response");
                return new AITaskAnalysisResult
                {
                    Summary = "Failed to parse AI response",
                    ConfidenceScore = 0.0
                };
            }
        }

        protected virtual AISuggestionsResult ParseSuggestionsResponse(string response)
        {
            try
            {
                var result = new AISuggestionsResult();
                
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
                _logger.LogWarning(ex, "Failed to parse AI suggestions response");
                return new AISuggestionsResult
                {
                    ConfidenceScore = 0.0
                };
            }
        }

        protected virtual List<string> ExtractKeywords(string text)
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