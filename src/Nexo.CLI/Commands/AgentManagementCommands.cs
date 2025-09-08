using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Services;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI commands for managing specialized AI agents
/// </summary>
public class AgentManagementCommands
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentManagementCommands> _logger;
    
    public AgentManagementCommands(IServiceProvider serviceProvider, ILogger<AgentManagementCommands> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Command CreateAgentListCommand()
    {
        var command = new Command("list", "List all available specialized AI agents");
        
        command.AddOption(new Option<bool>("--detailed", "Show detailed agent information"));
        command.AddOption(new Option<string>("--specialization", "Filter by specialization"));
        command.AddOption(new Option<string>("--platform", "Filter by platform"));
        
        command.Handler = CommandHandler.Create<bool, string, string>(async (detailed, specialization, platform) =>
        {
            await ListAgents(detailed, specialization, platform);
        });
        
        return command;
    }
    
    public Command CreateAgentAnalyzeCommand()
    {
        var command = new Command("analyze", "Analyze which agents would handle a request");
        
        command.AddArgument(new Argument<string>("request", "The request to analyze"));
        command.AddOption(new Option<string>("--platform", "Target platform"));
        command.AddOption(new Option<string>("--specialization", "Required specialization"));
        
        command.Handler = CommandHandler.Create<string, string, string>(async (request, platform, specialization) =>
        {
            await AnalyzeRequest(request, platform, specialization);
        });
        
        return command;
    }
    
    public Command CreateAgentPerformanceCommand()
    {
        var command = new Command("performance", "Show agent performance metrics");
        
        command.AddOption(new Option<string>("--agent-id", "Specific agent ID"));
        command.AddOption(new Option<int>("--days", () => 7, "Number of days to analyze"));
        command.AddOption(new Option<bool>("--detailed", "Show detailed metrics"));
        
        command.Handler = CommandHandler.Create<string, int, bool>(async (agentId, days, detailed) =>
        {
            await ShowPerformanceMetrics(agentId, days, detailed);
        });
        
        return command;
    }
    
    public Command CreateAgentTestCommand()
    {
        var command = new Command("test", "Test agent capabilities");
        
        command.AddArgument(new Argument<string>("agent-id", "Agent ID to test"));
        command.AddArgument(new Argument<string>("test-request", "Test request"));
        command.AddOption(new Option<bool>("--verbose", "Show detailed output"));
        
        command.Handler = CommandHandler.Create<string, string, bool>(async (agentId, testRequest, verbose) =>
        {
            await TestAgent(agentId, testRequest, verbose);
        });
        
        return command;
    }
    
    public Command CreateAgentRegistryCommand()
    {
        var command = new Command("registry", "Show agent registry statistics");
        
        command.AddOption(new Option<bool>("--detailed", "Show detailed statistics"));
        
        command.Handler = CommandHandler.Create<bool>(async (detailed) =>
        {
            await ShowRegistryStatistics(detailed);
        });
        
        return command;
    }
    
    private async Task ListAgents(bool detailed, string specialization, string platform)
    {
        try
        {
            var registry = _serviceProvider.GetRequiredService<ISpecializedAgentRegistry>();
            var agents = registry.GetAllAgents();
            
            // Apply filters
            if (!string.IsNullOrEmpty(specialization))
            {
                if (Enum.TryParse<AgentSpecialization>(specialization, true, out var spec))
                {
                    agents = agents.Where(a => a.Specialization.HasFlag(spec));
                }
                else
                {
                    Console.WriteLine($"Invalid specialization: {specialization}");
                    return;
                }
            }
            
            if (!string.IsNullOrEmpty(platform))
            {
                if (Enum.TryParse<PlatformCompatibility>(platform, true, out var plat))
                {
                    agents = agents.Where(a => a.PlatformExpertise.HasFlag(plat) || a.PlatformExpertise.HasFlag(PlatformCompatibility.All));
                }
                else
                {
                    Console.WriteLine($"Invalid platform: {platform}");
                    return;
                }
            }
            
            Console.WriteLine($"Found {agents.Count()} specialized AI agents:");
            Console.WriteLine();
            
            foreach (var agent in agents)
            {
                Console.WriteLine($"Agent ID: {agent.AgentId}");
                Console.WriteLine($"Specializations: {agent.Specialization}");
                Console.WriteLine($"Platform Expertise: {agent.PlatformExpertise}");
                Console.WriteLine($"Optimization Target: {agent.OptimizationProfile.PrimaryTarget}");
                
                if (detailed)
                {
                    Console.WriteLine($"Monitored Metrics: {string.Join(", ", agent.OptimizationProfile.MonitoredMetrics)}");
                    Console.WriteLine($"Real-time Optimization: {agent.OptimizationProfile.SupportsRealTimeOptimization}");
                    Console.WriteLine($"Minimum Performance Level: {agent.OptimizationProfile.MinimumAcceptableLevel}");
                }
                
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing agents");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private async Task AnalyzeRequest(string request, string platform, string specialization)
    {
        try
        {
            var registry = _serviceProvider.GetRequiredService<ISpecializedAgentRegistry>();
            
            var agentRequest = new AgentRequest
            {
                Input = request,
                TargetPlatform = !string.IsNullOrEmpty(platform) && Enum.TryParse<PlatformCompatibility>(platform, true, out var plat) ? plat : null,
                RequiredSpecialization = !string.IsNullOrEmpty(specialization) && Enum.TryParse<AgentSpecialization>(specialization, true, out var spec) ? spec : null
            };
            
            var bestAgents = registry.FindBestAgentsForRequest(agentRequest);
            
            Console.WriteLine($"Request Analysis: {request}");
            Console.WriteLine($"Target Platform: {platform ?? "Any"}");
            Console.WriteLine($"Required Specialization: {specialization ?? "Any"}");
            Console.WriteLine();
            
            if (!bestAgents.Any())
            {
                Console.WriteLine("No suitable agents found for this request.");
                return;
            }
            
            Console.WriteLine($"Found {bestAgents.Count()} suitable agents:");
            Console.WriteLine();
            
            foreach (var agent in bestAgents)
            {
                var assessment = await agent.AssessCapabilityAsync(agentRequest);
                
                Console.WriteLine($"Agent: {agent.AgentId}");
                Console.WriteLine($"Capability Score: {assessment.CapabilityScore:F2}");
                Console.WriteLine($"Can Handle Request: {assessment.CanHandleRequest}");
                Console.WriteLine($"Recommendation: {assessment.Recommendation}");
                
                if (assessment.Strengths.Any())
                {
                    Console.WriteLine($"Strengths: {string.Join(", ", assessment.Strengths)}");
                }
                
                if (assessment.Limitations.Any())
                {
                    Console.WriteLine($"Limitations: {string.Join(", ", assessment.Limitations)}");
                }
                
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing request");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private async Task ShowPerformanceMetrics(string agentId, int days, bool detailed)
    {
        try
        {
            var learningSystem = _serviceProvider.GetRequiredService<IAgentLearningSystem>();
            var metricsCollector = _serviceProvider.GetRequiredService<IPerformanceMetricsCollector>();
            
            if (!string.IsNullOrEmpty(agentId))
            {
                // Show metrics for specific agent
                var metrics = await metricsCollector.GetAgentMetricsAsync(agentId);
                var improvements = await learningSystem.GetRecommendedImprovements(agentId);
                
                Console.WriteLine($"Performance Metrics for Agent: {agentId}");
                Console.WriteLine($"Last Updated: {metrics.LastUpdated}");
                Console.WriteLine($"Success Rate: {metrics.SuccessRate:P2}");
                Console.WriteLine($"Average Response Time: {metrics.AverageResponseTime.TotalMilliseconds:F0}ms");
                Console.WriteLine($"Average Confidence: {metrics.AverageConfidence:F2}");
                Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
                Console.WriteLine($"Successful Requests: {metrics.SuccessfulRequests}");
                
                if (detailed)
                {
                    Console.WriteLine();
                    Console.WriteLine("Recommended Improvements:");
                    foreach (var improvement in improvements.RecommendedTraining)
                    {
                        Console.WriteLine($"- {improvement}");
                    }
                    
                    if (improvements.TopPerformingContexts.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Top Performing Contexts: {string.Join(", ", improvements.TopPerformingContexts)}");
                    }
                    
                    if (improvements.AreasForImprovement.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Areas for Improvement: {string.Join(", ", improvements.AreasForImprovement)}");
                    }
                }
            }
            else
            {
                // Show system-wide metrics
                var systemSummary = await metricsCollector.GetSystemPerformanceSummaryAsync();
                
                Console.WriteLine("System Performance Summary");
                Console.WriteLine($"Total Agents: {systemSummary.TotalAgents}");
                Console.WriteLine($"Overall Success Rate: {systemSummary.OverallSuccessRate:P2}");
                Console.WriteLine($"Average Response Time: {systemSummary.AverageResponseTime.TotalMilliseconds:F0}ms");
                Console.WriteLine($"High Performing Agents: {systemSummary.HighPerformingAgents}");
                Console.WriteLine($"Underperforming Agents: {systemSummary.UnderperformingAgents}");
                Console.WriteLine($"Total Requests: {systemSummary.TotalRequests}");
                Console.WriteLine($"Successful Requests: {systemSummary.SuccessfulRequests}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing performance metrics");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private async Task TestAgent(string agentId, string testRequest, bool verbose)
    {
        try
        {
            var registry = _serviceProvider.GetRequiredService<ISpecializedAgentRegistry>();
            var agent = registry.GetAgentById(agentId);
            
            if (agent == null)
            {
                Console.WriteLine($"Agent not found: {agentId}");
                return;
            }
            
            Console.WriteLine($"Testing Agent: {agentId}");
            Console.WriteLine($"Test Request: {testRequest}");
            Console.WriteLine();
            
            var request = new AgentRequest
            {
                Input = testRequest,
                Context = new Dictionary<string, object>
                {
                    ["TestMode"] = true,
                    ["Timestamp"] = DateTime.UtcNow
                }
            };
            
            // Assess capability
            var assessment = await agent.AssessCapabilityAsync(request);
            Console.WriteLine($"Capability Assessment:");
            Console.WriteLine($"  Score: {assessment.CapabilityScore:F2}");
            Console.WriteLine($"  Can Handle: {assessment.CanHandleRequest}");
            Console.WriteLine($"  Recommendation: {assessment.Recommendation}");
            Console.WriteLine();
            
            // Process request
            var startTime = DateTime.UtcNow;
            var response = await agent.ProcessAsync(request);
            var endTime = DateTime.UtcNow;
            
            Console.WriteLine($"Processing Results:");
            Console.WriteLine($"  Success: {response.Success}");
            Console.WriteLine($"  Confidence: {response.Confidence:F2}");
            Console.WriteLine($"  Processing Time: {(endTime - startTime).TotalMilliseconds:F0}ms");
            
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                Console.WriteLine($"  Error: {response.ErrorMessage}");
            }
            
            if (verbose && !string.IsNullOrEmpty(response.Result))
            {
                Console.WriteLine();
                Console.WriteLine("Generated Result:");
                Console.WriteLine(response.Result);
            }
            
            if (response.Metadata != null && response.Metadata.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Metadata:");
                foreach (var kvp in response.Metadata)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing agent");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private async Task ShowRegistryStatistics(bool detailed)
    {
        try
        {
            var registry = _serviceProvider.GetRequiredService<ISpecializedAgentRegistry>();
            var stats = registry.GetRegistryStatistics();
            
            Console.WriteLine("Agent Registry Statistics");
            Console.WriteLine($"Total Agents: {stats.TotalAgents}");
            Console.WriteLine($"Agents with Real-time Optimization: {stats.AgentsWithRealTimeOptimization}");
            Console.WriteLine();
            
            Console.WriteLine("Specializations:");
            foreach (var kvp in stats.SpecializationCounts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine();
            
            Console.WriteLine("Platform Expertise:");
            foreach (var kvp in stats.PlatformCounts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine();
            
            Console.WriteLine("Optimization Targets:");
            foreach (var kvp in stats.OptimizationTargetCounts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            if (detailed)
            {
                Console.WriteLine();
                Console.WriteLine("All Registered Agents:");
                var agents = registry.GetAllAgents();
                foreach (var agent in agents)
                {
                    Console.WriteLine($"  {agent.AgentId}: {agent.Specialization} | {agent.PlatformExpertise}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing registry statistics");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
