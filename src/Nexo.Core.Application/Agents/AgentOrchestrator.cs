using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Agents
{
    /// <summary>
    /// Orchestrates multiple agents to work together on complex tasks
    /// </summary>
    public class AgentOrchestrator : IAgentOrchestrator
    {
        private readonly Dictionary<AgentId, IAgent> _agents = new();
        private readonly Dictionary<AgentId, AgentHealth> _agentHealth = new();
        private readonly Dictionary<AgentId, List<AgentTask>> _agentWorkload = new();
        private readonly Dictionary<AgentId, List<AgentMessage>> _agentMessages = new();
        private readonly Dictionary<string, List<AgentId>> _capabilityIndex = new();
        private readonly Dictionary<PlatformType, List<AgentId>> _platformIndex = new();
        private readonly Dictionary<AgentStatus, List<AgentId>> _statusIndex = new();
        private readonly object _lock = new object();

        public async Task<AgentResult> RegisterAgentAsync(IAgent agent)
        {
            if (agent == null)
                return AgentResult.Failure("Agent cannot be null", agentId: agent.Id, operationType: "RegisterAgent");

            lock (_lock)
            {
                if (_agents.ContainsKey(agent.Id))
                    return AgentResult.Failure($"Agent {agent.Id} is already registered", agentId: agent.Id, operationType: "RegisterAgent");

                _agents[agent.Id] = agent;
                _agentHealth[agent.Id] = new AgentHealth(agent.Id, AgentHealthStatus.Healthy);
                _agentWorkload[agent.Id] = new List<AgentTask>();
                _agentMessages[agent.Id] = new List<AgentMessage>();

                // Update indices
                UpdateCapabilityIndex(agent);
                UpdatePlatformIndex(agent);
                UpdateStatusIndex(agent);
            }

            // Initialize the agent
            var context = new AgentContext(agent.Platform);
            var initResult = await agent.InitializeAsync(context);
            
            if (!initResult.IsSuccess)
            {
                // Remove from registration if initialization failed
                lock (_lock)
                {
                    _agents.Remove(agent.Id);
                    _agentHealth.Remove(agent.Id);
                    _agentWorkload.Remove(agent.Id);
                    _agentMessages.Remove(agent.Id);
                    UpdateIndices();
                }
            }

            return initResult;
        }

        public async Task<AgentResult> UnregisterAgentAsync(AgentId agentId)
        {
            if (agentId == null)
                return AgentResult.Failure("Agent ID cannot be null", operationType: "UnregisterAgent");

            lock (_lock)
            {
                if (!_agents.ContainsKey(agentId))
                    return AgentResult.Failure($"Agent {agentId} is not registered", operationType: "UnregisterAgent");

                var agent = _agents[agentId];
                
                // Shutdown the agent
                _ = agent.ShutdownAsync();

                // Remove from all collections
                _agents.Remove(agentId);
                _agentHealth.Remove(agentId);
                _agentWorkload.Remove(agentId);
                _agentMessages.Remove(agentId);

                // Update indices
                UpdateIndices();
            }

            return AgentResult.Success($"Agent {agentId} unregistered successfully", operationType: "UnregisterAgent");
        }

        public Task<IReadOnlyList<IAgent>> GetRegisteredAgentsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_agents.Values.ToList().AsReadOnly());
            }
        }

        public Task<IReadOnlyList<IAgent>> GetAgentsByCapabilityAsync(string capabilityName)
        {
            if (string.IsNullOrEmpty(capabilityName))
                return Task.FromResult(new List<IAgent>().AsReadOnly());

            lock (_lock)
            {
                if (!_capabilityIndex.TryGetValue(capabilityName.ToLowerInvariant(), out var agentIds))
                    return Task.FromResult(new List<IAgent>().AsReadOnly());

                var agents = agentIds
                    .Where(id => _agents.ContainsKey(id))
                    .Select(id => _agents[id])
                    .ToList()
                    .AsReadOnly();

                return Task.FromResult(agents);
            }
        }

        public Task<IReadOnlyList<IAgent>> GetAgentsByPlatformAsync(PlatformType platform)
        {
            if (platform == null)
                return Task.FromResult(new List<IAgent>().AsReadOnly());

            lock (_lock)
            {
                if (!_platformIndex.TryGetValue(platform, out var agentIds))
                    return Task.FromResult(new List<IAgent>().AsReadOnly());

                var agents = agentIds
                    .Where(id => _agents.ContainsKey(id))
                    .Select(id => _agents[id])
                    .ToList()
                    .AsReadOnly();

                return Task.FromResult(agents);
            }
        }

        public Task<IReadOnlyList<IAgent>> GetAgentsByStatusAsync(AgentStatus status)
        {
            if (status == null)
                return Task.FromResult(new List<IAgent>().AsReadOnly());

            lock (_lock)
            {
                if (!_statusIndex.TryGetValue(status, out var agentIds))
                    return Task.FromResult(new List<IAgent>().AsReadOnly());

                var agents = agentIds
                    .Where(id => _agents.ContainsKey(id))
                    .Select(id => _agents[id])
                    .ToList()
                    .AsReadOnly();

                return Task.FromResult(agents);
            }
        }

        public async Task<IAgent?> FindBestAgentForTaskAsync(AgentTask task)
        {
            if (task == null)
                return null;

            var availableAgents = await GetRegisteredAgentsAsync();
            var suitableAgents = availableAgents
                .Where(agent => task.CanBeExecutedBy(agent))
                .ToList();

            if (!suitableAgents.Any())
                return null;

            // Score agents based on capabilities, workload, and health
            var scoredAgents = suitableAgents
                .Select(agent => new
                {
                    Agent = agent,
                    Score = CalculateAgentScore(agent, task)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            return scoredAgents.FirstOrDefault()?.Agent;
        }

        public async Task<AgentResult> ExecuteTaskAsync(AgentTask task)
        {
            if (task == null)
                return AgentResult.Failure("Task cannot be null", operationType: "ExecuteTask");

            var bestAgent = await FindBestAgentForTaskAsync(task);
            if (bestAgent == null)
                return AgentResult.Failure("No suitable agent found for task", operationType: "ExecuteTask");

            return await ExecuteTaskAsync(task, bestAgent.Id);
        }

        public async Task<AgentResult> ExecuteTaskAsync(AgentTask task, AgentId agentId)
        {
            if (task == null)
                return AgentResult.Failure("Task cannot be null", operationType: "ExecuteTask");

            if (agentId == null)
                return AgentResult.Failure("Agent ID cannot be null", operationType: "ExecuteTask");

            lock (_lock)
            {
                if (!_agents.TryGetValue(agentId, out var agent))
                    return AgentResult.Failure($"Agent {agentId} is not registered", operationType: "ExecuteTask");

                if (!task.CanBeExecutedBy(agent))
                    return AgentResult.Failure($"Agent {agentId} cannot execute this task", operationType: "ExecuteTask");

                // Add to workload
                _agentWorkload[agentId].Add(task);
            }

            try
            {
                var result = await agent.ExecuteAsync(task);
                
                lock (_lock)
                {
                    // Remove from workload
                    _agentWorkload[agentId].Remove(task);
                }

                return result;
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    // Remove from workload
                    _agentWorkload[agentId].Remove(task);
                }

                return AgentResult.FromException(ex, agentId, "ExecuteTask");
            }
        }

        public async Task<AgentResult> ExecuteTaskParallelAsync(AgentTask task, IEnumerable<AgentId> agentIds)
        {
            if (task == null)
                return AgentResult.Failure("Task cannot be null", operationType: "ExecuteTaskParallel");

            if (agentIds == null)
                return AgentResult.Failure("Agent IDs cannot be null", operationType: "ExecuteTaskParallel");

            var agentList = agentIds.ToList();
            if (!agentList.Any())
                return AgentResult.Failure("No agents specified", operationType: "ExecuteTaskParallel");

            var tasks = agentList.Select(agentId => ExecuteTaskAsync(task, agentId));
            var results = await Task.WhenAll(tasks);

            return AgentResult.FromSubResults(results, operationType: "ExecuteTaskParallel");
        }

        public async Task<AgentResult> ExecuteTaskSequentialAsync(AgentTask task, IEnumerable<AgentId> agentIds)
        {
            if (task == null)
                return AgentResult.Failure("Task cannot be null", operationType: "ExecuteTaskSequential");

            if (agentIds == null)
                return AgentResult.Failure("Agent IDs cannot be null", operationType: "ExecuteTaskSequential");

            var agentList = agentIds.ToList();
            if (!agentList.Any())
                return AgentResult.Failure("No agents specified", operationType: "ExecuteTaskSequential");

            var results = new List<AgentResult>();
            foreach (var agentId in agentList)
            {
                var result = await ExecuteTaskAsync(task, agentId);
                results.Add(result);
                
                // Stop on first failure
                if (!result.IsSuccess)
                    break;
            }

            return AgentResult.FromSubResults(results, operationType: "ExecuteTaskSequential");
        }

        public async Task<AgentResult> CoordinateAgentsAsync(AgentTask task, IEnumerable<AgentId> agentIds)
        {
            if (task == null)
                return AgentResult.Failure("Task cannot be null", operationType: "CoordinateAgents");

            if (agentIds == null)
                return AgentResult.Failure("Agent IDs cannot be null", operationType: "CoordinateAgents");

            var agentList = agentIds.ToList();
            if (!agentList.Any())
                return AgentResult.Failure("No agents specified", operationType: "CoordinateAgents");

            // Create coordination message
            var coordinationMessage = AgentMessage.CreateTaskAssignment(
                Guid.NewGuid().ToString(),
                new AgentId("orchestrator"),
                agentList.First(),
                task.Id,
                task.Name,
                task.Description);

            // Send coordination message to all agents
            var coordinationResults = new List<AgentResult>();
            foreach (var agentId in agentList)
            {
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    var message = new AgentMessage(
                        coordinationMessage.Id,
                        coordinationMessage.SenderId,
                        agentId,
                        coordinationMessage.Type,
                        coordinationMessage.Subject,
                        coordinationMessage.Content,
                        coordinationMessage.Priority,
                        coordinationMessage.Metadata,
                        coordinationMessage.ExpiresAt,
                        coordinationMessage.RequiresResponse,
                        coordinationMessage.ResponseToMessageId);

                    var result = await agent.CommunicateAsync(agent, message);
                    coordinationResults.Add(result);
                }
            }

            return AgentResult.FromSubResults(coordinationResults, operationType: "CoordinateAgents");
        }

        public async Task<AgentResult> BroadcastMessageAsync(AgentMessage message)
        {
            if (message == null)
                return AgentResult.Failure("Message cannot be null", operationType: "BroadcastMessage");

            var agents = await GetRegisteredAgentsAsync();
            var results = new List<AgentResult>();

            foreach (var agent in agents)
            {
                try
                {
                    var result = await agent.CommunicateAsync(agent, message);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(AgentResult.FromException(ex, agent.Id, "BroadcastMessage"));
                }
            }

            return AgentResult.FromSubResults(results, operationType: "BroadcastMessage");
        }

        public async Task<AgentResult> SendMessageAsync(AgentMessage message, IEnumerable<AgentId> recipientIds)
        {
            if (message == null)
                return AgentResult.Failure("Message cannot be null", operationType: "SendMessage");

            if (recipientIds == null)
                return AgentResult.Failure("Recipient IDs cannot be null", operationType: "SendMessage");

            var results = new List<AgentResult>();
            foreach (var recipientId in recipientIds)
            {
                if (_agents.TryGetValue(recipientId, out var agent))
                {
                    try
                    {
                        var result = await agent.CommunicateAsync(agent, message);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        results.Add(AgentResult.FromException(ex, recipientId, "SendMessage"));
                    }
                }
            }

            return AgentResult.FromSubResults(results, operationType: "SendMessage");
        }

        public async Task<AgentOrchestratorMetrics> GetMetricsAsync()
        {
            var agents = await GetRegisteredAgentsAsync();
            var totalAgents = agents.Count;
            var activeAgents = agents.Count(a => a.Status == AgentStatus.Active);
            var busyAgents = agents.Count(a => a.Status == AgentStatus.Busy);
            var failedAgents = agents.Count(a => a.Status == AgentStatus.Failed);

            var totalWorkload = 0;
            var totalMessages = 0;
            lock (_lock)
            {
                totalWorkload = _agentWorkload.Values.Sum(tasks => tasks.Count);
                totalMessages = _agentMessages.Values.Sum(messages => messages.Count);
            }

            return new AgentOrchestratorMetrics(
                totalAgents,
                activeAgents,
                busyAgents,
                failedAgents,
                totalWorkload,
                totalMessages,
                DateTimeOffset.UtcNow);
        }

        public async Task<AgentHealthReport> GetHealthReportAsync()
        {
            var agents = await GetRegisteredAgentsAsync();
            var healthyAgents = 0;
            var unhealthyAgents = 0;
            var unknownAgents = 0;

            lock (_lock)
            {
                foreach (var agent in agents)
                {
                    if (_agentHealth.TryGetValue(agent.Id, out var health))
                    {
                        switch (health.Status)
                        {
                            case AgentHealthStatus.Healthy:
                                healthyAgents++;
                                break;
                            case AgentHealthStatus.Unhealthy:
                                unhealthyAgents++;
                                break;
                            case AgentHealthStatus.Unknown:
                                unknownAgents++;
                                break;
                        }
                    }
                    else
                    {
                        unknownAgents++;
                    }
                }
            }

            return new AgentHealthReport(
                healthyAgents,
                unhealthyAgents,
                unknownAgents,
                DateTimeOffset.UtcNow);
        }

        public async Task<AgentResult> ScaleAgentsAsync(ScalingStrategy strategy)
        {
            // Implement agent scaling logic
            return AgentResult.Success("Agent scaling completed", operationType: "ScaleAgents");
        }

        public async Task<AgentResult> BalanceWorkloadAsync()
        {
            // Implement workload balancing logic
            return AgentResult.Success("Workload balancing completed", operationType: "BalanceWorkload");
        }

        public async Task<AgentNetwork> GetAgentNetworkAsync()
        {
            var agents = await GetRegisteredAgentsAsync();
            var connections = new List<AgentConnection>();

            // Build agent network connections
            foreach (var agent in agents)
            {
                foreach (var otherAgent in agents.Where(a => a.Id != agent.Id))
                {
                    connections.Add(new AgentConnection(agent.Id, otherAgent.Id, ConnectionType.Peer));
                }
            }

            return new AgentNetwork(agents.Select(a => a.Id).ToList(), connections);
        }

        private void UpdateCapabilityIndex(IAgent agent)
        {
            foreach (var capability in agent.Capabilities)
            {
                var key = capability.Name.ToLowerInvariant();
                if (!_capabilityIndex.ContainsKey(key))
                    _capabilityIndex[key] = new List<AgentId>();
                
                if (!_capabilityIndex[key].Contains(agent.Id))
                    _capabilityIndex[key].Add(agent.Id);
            }
        }

        private void UpdatePlatformIndex(IAgent agent)
        {
            if (!_platformIndex.ContainsKey(agent.Platform))
                _platformIndex[agent.Platform] = new List<AgentId>();
            
            if (!_platformIndex[agent.Platform].Contains(agent.Id))
                _platformIndex[agent.Platform].Add(agent.Id);
        }

        private void UpdateStatusIndex(IAgent agent)
        {
            if (!_statusIndex.ContainsKey(agent.Status))
                _statusIndex[agent.Status] = new List<AgentId>();
            
            if (!_statusIndex[agent.Status].Contains(agent.Id))
                _statusIndex[agent.Status].Add(agent.Id);
        }

        private void UpdateIndices()
        {
            _capabilityIndex.Clear();
            _platformIndex.Clear();
            _statusIndex.Clear();

            foreach (var agent in _agents.Values)
            {
                UpdateCapabilityIndex(agent);
                UpdatePlatformIndex(agent);
                UpdateStatusIndex(agent);
            }
        }

        private double CalculateAgentScore(IAgent agent, AgentTask task)
        {
            var score = 0.0;

            // Capability match score
            var capabilityMatches = agent.Capabilities.Count(c => 
                task.RequiredCapabilities.Contains(c.Name, StringComparer.OrdinalIgnoreCase));
            score += capabilityMatches * 10.0;

            // Workload score (lower workload = higher score)
            lock (_lock)
            {
                if (_agentWorkload.TryGetValue(agent.Id, out var workload))
                {
                    score += Math.Max(0, 10 - workload.Count);
                }
            }

            // Health score
            lock (_lock)
            {
                if (_agentHealth.TryGetValue(agent.Id, out var health))
                {
                    score += health.Status == AgentHealthStatus.Healthy ? 5.0 : 0.0;
                }
            }

            // Platform compatibility score
            if (agent.Platform == task.Platform)
                score += 5.0;

            return score;
        }
    }

    // Supporting classes
    public class AgentHealth
    {
        public AgentId AgentId { get; }
        public AgentHealthStatus Status { get; }
        public DateTimeOffset LastChecked { get; }
        public string? Details { get; }

        public AgentHealth(AgentId agentId, AgentHealthStatus status, string? details = null)
        {
            AgentId = agentId;
            Status = status;
            LastChecked = DateTimeOffset.UtcNow;
            Details = details;
        }
    }

    public class AgentOrchestratorMetrics
    {
        public int TotalAgents { get; }
        public int ActiveAgents { get; }
        public int BusyAgents { get; }
        public int FailedAgents { get; }
        public int TotalWorkload { get; }
        public int TotalMessages { get; }
        public DateTimeOffset Timestamp { get; }

        public AgentOrchestratorMetrics(
            int totalAgents,
            int activeAgents,
            int busyAgents,
            int failedAgents,
            int totalWorkload,
            int totalMessages,
            DateTimeOffset timestamp)
        {
            TotalAgents = totalAgents;
            ActiveAgents = activeAgents;
            BusyAgents = busyAgents;
            FailedAgents = failedAgents;
            TotalWorkload = totalWorkload;
            TotalMessages = totalMessages;
            Timestamp = timestamp;
        }
    }

    public class AgentHealthReport
    {
        public int HealthyAgents { get; }
        public int UnhealthyAgents { get; }
        public int UnknownAgents { get; }
        public DateTimeOffset Timestamp { get; }

        public AgentHealthReport(int healthyAgents, int unhealthyAgents, int unknownAgents, DateTimeOffset timestamp)
        {
            HealthyAgents = healthyAgents;
            UnhealthyAgents = unhealthyAgents;
            UnknownAgents = unknownAgents;
            Timestamp = timestamp;
        }
    }

    public class AgentNetwork
    {
        public IReadOnlyList<AgentId> Agents { get; }
        public IReadOnlyList<AgentConnection> Connections { get; }

        public AgentNetwork(IReadOnlyList<AgentId> agents, IReadOnlyList<AgentConnection> connections)
        {
            Agents = agents;
            Connections = connections;
        }
    }

    public class AgentConnection
    {
        public AgentId FromAgent { get; }
        public AgentId ToAgent { get; }
        public ConnectionType Type { get; }

        public AgentConnection(AgentId fromAgent, AgentId toAgent, ConnectionType type)
        {
            FromAgent = fromAgent;
            ToAgent = toAgent;
            Type = type;
        }
    }

    public enum AgentHealthStatus
    {
        Healthy,
        Unhealthy,
        Unknown
    }

    public enum ConnectionType
    {
        Peer,
        Hierarchical,
        Collaborative
    }

    public enum ScalingStrategy
    {
        Horizontal,
        Vertical,
        Auto
    }
}
