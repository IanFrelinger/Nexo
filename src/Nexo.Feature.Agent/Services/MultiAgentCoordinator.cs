using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Multi-agent coordinator for agent-to-agent communication and collaboration.
    /// </summary>
    public class MultiAgentCoordinator : IMultiAgentCoordinator
    {
        private readonly ILogger<MultiAgentCoordinator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly Dictionary<string, IAIEnhancedAgent> _registeredAgents;
        private readonly Dictionary<string, AgentCapabilityProfile> _agentCapabilities;
        private readonly List<CollaborationSession> _activeSessions;
        private readonly object _agentsLock = new object();

        public MultiAgentCoordinator(
            IModelOrchestrator modelOrchestrator,
            ILogger<MultiAgentCoordinator> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _registeredAgents = new Dictionary<string, IAIEnhancedAgent>();
            _agentCapabilities = new Dictionary<string, AgentCapabilityProfile>();
            _activeSessions = new List<CollaborationSession>();
        }

        /// <summary>
        /// Registers an agent with the coordinator.
        /// </summary>
        public void RegisterAgent(IAIEnhancedAgent agent)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            
            lock (_agentsLock)
            {
                var agentId = agent.Id.Value;
                _registeredAgents[agentId] = agent;
                
                // Create capability profile
                _agentCapabilities[agentId] = new AgentCapabilityProfile
                {
                    AgentId = agentId,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Capabilities = agent.Capabilities.ToList(),
                    FocusAreas = agent.FocusAreas.ToList(),
                    AICapabilities = agent.AICapabilities
                };
                
                _logger.LogInformation("Registered agent: {AgentName} ({AgentId})", agent.Name.Value, agentId);
            }
        }

        /// <summary>
        /// Unregisters an agent from the coordinator.
        /// </summary>
        public void UnregisterAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId)) throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            
            lock (_agentsLock)
            {
                if (_registeredAgents.Remove(agentId))
                {
                    _agentCapabilities.Remove(agentId);
                    _logger.LogInformation("Unregistered agent: {AgentId}", agentId);
                }
            }
        }

        /// <summary>
        /// Creates a collaboration session between multiple agents.
        /// </summary>
        public async Task<CollaborationSession> CreateCollaborationSessionAsync(CollaborationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating collaboration session: {SessionName}", request.SessionName);

            try
            {
                // Select agents based on requirements
                var selectedAgents = await SelectAgentsForCollaborationAsync(request, cancellationToken);
                
                if (!selectedAgents.Any())
                {
                    throw new InvalidOperationException("No suitable agents found for the collaboration request");
                }

                var session = new CollaborationSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    SessionName = request.SessionName,
                    Description = request.Description,
                    ParticipatingAgents = selectedAgents,
                    SessionType = request.SessionType,
                    Status = CollaborationSessionStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    Configuration = request.Configuration
                };

                lock (_agentsLock)
                {
                    _activeSessions.Add(session);
                }

                _logger.LogInformation("Created collaboration session {SessionId} with {AgentCount} agents", 
                    session.SessionId, selectedAgents.Count);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collaboration session");
                throw;
            }
        }

        /// <summary>
        /// Executes a collaborative task with multiple agents.
        /// </summary>
        public async Task<CollaborationResult> ExecuteCollaborativeTaskAsync(CollaborativeTask task, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing collaborative task: {TaskName}", task.TaskName);

            try
            {
                var session = await CreateCollaborationSessionAsync(new CollaborationRequest
                {
                    SessionName = $"Task_{task.TaskName}_{DateTime.UtcNow.Ticks}",
                    Description = task.Description,
                    SessionType = CollaborationSessionType.TaskExecution,
                    RequiredCapabilities = task.RequiredCapabilities,
                    RequiredRoles = task.RequiredRoles,
                    Configuration = task.Configuration
                }, cancellationToken);

                var result = new CollaborationResult
                {
                    SessionId = session.SessionId,
                    TaskName = task.TaskName,
                    Success = true,
                    AgentResults = new List<AgentTaskResult>(),
                    CollaborationMetrics = new CollaborationMetrics()
                };

                var startTime = DateTime.UtcNow;

                // Execute task with participating agents
                foreach (var agent in session.ParticipatingAgents)
                {
                    var agentResult = await ExecuteTaskWithAgentAsync(agent, task, cancellationToken);
                    result.AgentResults.Add(agentResult);
                }

                // Coordinate and synthesize results
                var synthesisResult = await SynthesizeAgentResultsAsync(result.AgentResults, task, cancellationToken);
                result.SynthesizedResult = synthesisResult;
                result.CollaborationMetrics.TotalProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.CollaborationMetrics.AgentCount = session.ParticipatingAgents.Count;

                // Update session status
                session.Status = CollaborationSessionStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Completed collaborative task {TaskName} with {AgentCount} agents", 
                    task.TaskName, session.ParticipatingAgents.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing collaborative task: {TaskName}", task.TaskName);
                return new CollaborationResult
                {
                    TaskName = task.TaskName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Facilitates agent-to-agent communication.
        /// </summary>
        public async Task<AgentCommunicationResult> FacilitateCommunicationAsync(AgentCommunicationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Facilitating communication from {SenderId} to {RecipientId}", 
                request.SenderAgentId, request.RecipientAgentId);

            try
            {
                // Validate agents exist
                if (!_registeredAgents.ContainsKey(request.SenderAgentId))
                {
                    throw new ArgumentException($"Sender agent {request.SenderAgentId} not found");
                }

                if (!_registeredAgents.ContainsKey(request.RecipientAgentId))
                {
                    throw new ArgumentException($"Recipient agent {request.RecipientAgentId} not found");
                }

                var sender = _registeredAgents[request.SenderAgentId];
                var recipient = _registeredAgents[request.RecipientAgentId];

                // Create communication context
                var context = new AgentCommunicationContext
                {
                    CommunicationId = Guid.NewGuid().ToString(),
                    SenderAgent = sender,
                    RecipientAgent = recipient,
                    Message = request.Message,
                    MessageType = request.MessageType,
                    Priority = request.Priority,
                    Timestamp = DateTime.UtcNow
                };

                // Process communication through recipient agent
                var aiRequest = new AIEnhancedAgentRequest
                {
                    Type = AgentRequestType.Communication,
                    Content = request.Message,
                    UseAI = true,
                    AIContext = new Dictionary<string, object>
                    {
                        ["senderAgent"] = sender.Name.Value,
                        ["senderRole"] = sender.Role.Value,
                        ["messageType"] = request.MessageType.ToString(),
                        ["priority"] = request.Priority.ToString()
                    }
                };

                var response = await recipient.ProcessAIRequestAsync(aiRequest, cancellationToken);

                return new AgentCommunicationResult
                {
                    CommunicationId = context.CommunicationId,
                    Success = response.Success,
                    Response = response.Content,
                    ProcessingTimeMs = response.AIProcessingTimeMs,
                    AIWasUsed = response.AIWasUsed,
                    AIModelUsed = response.AIModelUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error facilitating agent communication");
                return new AgentCommunicationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Analyzes collaboration patterns and provides insights.
        /// </summary>
        public async Task<CollaborationAnalysisResult> AnalyzeCollaborationPatternsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing collaboration patterns");

            try
            {
                var analysis = new CollaborationAnalysisResult
                {
                    Success = true,
                    AnalysisTimestamp = DateTime.UtcNow,
                    ActiveSessionsCount = _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Active),
                    CompletedSessionsCount = _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Completed),
                    RegisteredAgentsCount = _registeredAgents.Count
                };

                // Analyze agent collaboration patterns
                analysis.AgentCollaborationPatterns = AnalyzeAgentCollaborationPatterns();
                
                // Analyze session performance
                analysis.SessionPerformanceMetrics = AnalyzeSessionPerformance();
                
                // Generate collaboration recommendations
                analysis.Recommendations = GenerateCollaborationRecommendations(analysis);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing collaboration patterns");
                return new CollaborationAnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets all registered agents.
        /// </summary>
        public List<IAIEnhancedAgent> GetRegisteredAgents()
        {
            lock (_agentsLock)
            {
                return _registeredAgents.Values.ToList();
            }
        }

        /// <summary>
        /// Gets agent capabilities by ID.
        /// </summary>
        public AgentCapabilityProfile GetAgentCapabilities(string agentId)
        {
            lock (_agentsLock)
            {
                return _agentCapabilities.ContainsKey(agentId) ? _agentCapabilities[agentId] : new AgentCapabilityProfile();
            }
        }

        /// <summary>
        /// Gets active collaboration sessions.
        /// </summary>
        public List<CollaborationSession> GetActiveSessions()
        {
            lock (_agentsLock)
            {
                return _activeSessions.Where(s => s.Status == CollaborationSessionStatus.Active).ToList();
            }
        }

        private async Task<List<IAIEnhancedAgent>> SelectAgentsForCollaborationAsync(CollaborationRequest request, CancellationToken cancellationToken)
        {
            var candidates = new List<AgentCandidate>();

            lock (_agentsLock)
            {
                foreach (var kvp in _registeredAgents)
                {
                    var agent = kvp.Value;
                    var capabilities = _agentCapabilities[kvp.Key];
                    
                    var score = EvaluateAgentForCollaboration(capabilities, request);
                    candidates.Add(new AgentCandidate { Agent = agent, Score = score });
                }
            }

            // Sort by score and select top agents
            var selectedAgents = candidates
                .OrderByDescending(c => c.Score)
                .Take(request.MaxAgents)
                .Select(c => c.Agent)
                .ToList();

            _logger.LogDebug("Selected {Count} agents for collaboration with scores: {Scores}", 
                selectedAgents.Count, string.Join(", ", candidates.Take(request.MaxAgents).Select(c => $"{c.Agent.Name.Value}:{c.Score:F2}")));

            return selectedAgents;
        }

        private double EvaluateAgentForCollaboration(AgentCapabilityProfile capabilities, CollaborationRequest request)
        {
            double score = 0.0;
            int totalCriteria = 0;

            // Check capability matching
            if (request.RequiredCapabilities.Any())
            {
                var matchingCapabilities = capabilities.Capabilities
                    .Count(cap => request.RequiredCapabilities.Contains(cap));
                score += (double)matchingCapabilities / request.RequiredCapabilities.Count;
                totalCriteria++;
            }

            // Check role matching
            if (request.RequiredRoles.Any())
            {
                if (request.RequiredRoles.Contains(capabilities.AgentRole))
                {
                    score += 1.0;
                }
                totalCriteria++;
            }

            // Check AI capabilities
            if (request.RequireAICapabilities && capabilities.AICapabilities.CanAnalyzeTasks)
            {
                score += 1.0;
                totalCriteria++;
            }

            // Check availability (simple heuristic)
            score += 0.5; // Assume agents are available
            totalCriteria++;

            return totalCriteria > 0 ? score / totalCriteria : 0.0;
        }

        private async Task<AgentTaskResult> ExecuteTaskWithAgentAsync(IAIEnhancedAgent agent, CollaborativeTask task, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var aiRequest = new AIEnhancedAgentRequest
                {
                    Type = AgentRequestType.Collaboration,
                    Content = task.Description,
                    UseAI = true,
                    AIContext = new Dictionary<string, object>
                    {
                        ["taskName"] = task.TaskName,
                        ["taskType"] = task.TaskType,
                        ["collaborationMode"] = "true",
                        ["agentRole"] = agent.Role.Value
                    }
                };

                var response = await agent.ProcessAIRequestAsync(aiRequest, cancellationToken);

                return new AgentTaskResult
                {
                    AgentId = agent.Id.Value,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Success = response.Success,
                    Content = response.Content,
                    ProcessingTimeMs = response.AIProcessingTimeMs,
                    AIWasUsed = response.AIWasUsed,
                    AIModelUsed = response.AIModelUsed,
                    ConfidenceScore = response.AIConfidenceScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing task with agent {AgentName}", agent.Name.Value);
                return new AgentTaskResult
                {
                    AgentId = agent.Id.Value,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> SynthesizeAgentResultsAsync(List<AgentTaskResult> agentResults, CollaborativeTask task, CancellationToken cancellationToken)
        {
            if (!agentResults.Any(r => r.Success))
            {
                return "No successful agent results to synthesize.";
            }

            var successfulResults = agentResults.Where(r => r.Success).ToList();
            
            // Create synthesis prompt
            var synthesisPrompt = $@"Synthesize the following collaborative task results:

Task: {task.TaskName}
Description: {task.Description}

Agent Results:
{string.Join("\n\n", successfulResults.Select(r => $"Agent: {r.AgentName} ({r.AgentRole})\nResult: {r.Content}"))}

Please provide a comprehensive synthesis that:
1. Identifies common themes and patterns
2. Resolves any conflicts or contradictions
3. Provides a unified, coherent response
4. Highlights the most valuable insights from each agent
5. Suggests next steps or recommendations";

            var synthesisRequest = new ModelRequest
            {
                Input = synthesisPrompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var synthesisResponse = await _modelOrchestrator.ExecuteAsync(synthesisRequest, cancellationToken);
            return synthesisResponse.Content;
        }

        private List<AgentCollaborationPattern> AnalyzeAgentCollaborationPatterns()
        {
            var patterns = new List<AgentCollaborationPattern>();

            // Analyze which agents work well together
            var agentCollaborations = _activeSessions
                .Where(s => s.Status == CollaborationSessionStatus.Completed)
                .SelectMany(s => s.ParticipatingAgents)
                .GroupBy(a => a.Id.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in agentCollaborations)
            {
                patterns.Add(new AgentCollaborationPattern
                {
                    AgentId = kvp.Key,
                    CollaborationCount = kvp.Value,
                    CollaborationFrequency = (double)kvp.Value / _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Completed)
                });
            }

            return patterns;
        }

        private SessionPerformanceMetrics AnalyzeSessionPerformance()
        {
            var completedSessions = _activeSessions.Where(s => s.Status == CollaborationSessionStatus.Completed).ToList();
            
            if (!completedSessions.Any())
            {
                return new SessionPerformanceMetrics();
            }

            return new SessionPerformanceMetrics
            {
                AverageSessionDuration = completedSessions
                    .Where(s => s.CompletedAt.HasValue)
                    .Average(s => (s.CompletedAt.Value - s.CreatedAt).TotalMilliseconds),
                AverageAgentsPerSession = completedSessions.Average(s => s.ParticipatingAgents.Count),
                SuccessRate = (double)completedSessions.Count(s => s.Status == CollaborationSessionStatus.Completed) / completedSessions.Count
            };
        }

        private List<CollaborationRecommendation> GenerateCollaborationRecommendations(CollaborationAnalysisResult analysis)
        {
            var recommendations = new List<CollaborationRecommendation>();

            // Recommend optimal agent combinations
            if (analysis.AgentCollaborationPatterns.Any())
            {
                var topCollaborators = analysis.AgentCollaborationPatterns
                    .OrderByDescending(p => p.CollaborationFrequency)
                    .Take(3);

                recommendations.Add(new CollaborationRecommendation
                {
                    Type = RecommendationType.AgentCombination,
                    Description = $"Consider pairing agents: {string.Join(", ", topCollaborators.Select(p => p.AgentId))}",
                    Priority = RecommendationPriority.Medium,
                    EstimatedImpact = "High"
                });
            }

            // Recommend session optimization
            if (analysis.SessionPerformanceMetrics.AverageSessionDuration > 300000) // 5 minutes
            {
                recommendations.Add(new CollaborationRecommendation
                {
                    Type = RecommendationType.Performance,
                    Description = "Consider optimizing session duration by reducing agent count or simplifying tasks",
                    Priority = RecommendationPriority.High,
                    EstimatedImpact = "Medium"
                });
            }

            return recommendations;
        }
    }

} 