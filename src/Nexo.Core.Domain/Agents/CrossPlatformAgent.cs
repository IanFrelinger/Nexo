using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Base implementation of a cross-platform AI agent
    /// </summary>
    public abstract class CrossPlatformAgent : IAgent
    {
        protected readonly List<AgentCapability> _capabilities = new();
        protected readonly List<string> _focusAreas = new();
        protected readonly Dictionary<string, object> _configuration = new();
        protected readonly List<AgentExperience> _experiences = new();
        protected readonly Dictionary<string, object> _runtimeData = new();

        public AgentId Id { get; }
        public AgentName Name { get; }
        public AgentStatus Status { get; protected set; }
        public PlatformType Platform { get; }
        public IReadOnlyList<AgentCapability> Capabilities => _capabilities.AsReadOnly();
        public IReadOnlyList<string> FocusAreas => _focusAreas.AsReadOnly();
        public IReadOnlyDictionary<string, object> Configuration => _configuration.AsReadOnly();

        protected CrossPlatformAgent(
            AgentId id,
            AgentName name,
            PlatformType platform,
            IEnumerable<AgentCapability>? capabilities = null,
            IEnumerable<string>? focusAreas = null,
            IDictionary<string, object>? configuration = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Status = AgentStatus.Inactive;

            if (capabilities != null)
            {
                _capabilities.AddRange(capabilities);
            }

            if (focusAreas != null)
            {
                _focusAreas.AddRange(focusAreas);
            }

            if (configuration != null)
            {
                foreach (var kvp in configuration)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
        }

        public virtual async Task<AgentResult> InitializeAsync(AgentContext context)
        {
            try
            {
                Status = AgentStatus.Active;
                await OnInitializeAsync(context);
                return AgentResult.Success(
                    "Agent initialized successfully",
                    new Dictionary<string, object>
                    {
                        ["platform"] = Platform.Name,
                        ["capabilities"] = _capabilities.Count,
                        ["focusAreas"] = _focusAreas.Count
                    },
                    agentId: Id,
                    operationType: "Initialize");
            }
            catch (Exception ex)
            {
                Status = AgentStatus.Failed;
                return AgentResult.FromException(ex, Id, "Initialize");
            }
        }

        public virtual async Task<AgentResult> ExecuteAsync(AgentTask task)
        {
            if (Status != AgentStatus.Active)
            {
                return AgentResult.Failure(
                    "Agent is not active",
                    agentId: Id,
                    operationType: "Execute");
            }

            if (!task.CanBeExecutedBy(this))
            {
                return AgentResult.Failure(
                    "Agent cannot execute this task",
                    new Dictionary<string, object>
                    {
                        ["requiredCapabilities"] = task.RequiredCapabilities,
                        ["agentCapabilities"] = _capabilities.Select(c => c.Name).ToList()
                    },
                    agentId: Id,
                    operationType: "Execute");
            }

            try
            {
                Status = AgentStatus.Busy;
                var result = await OnExecuteAsync(task);
                Status = AgentStatus.Active;
                return result;
            }
            catch (Exception ex)
            {
                Status = AgentStatus.Failed;
                return AgentResult.FromException(ex, Id, "Execute");
            }
        }

        public virtual async Task<AgentResult> CommunicateAsync(IAgent targetAgent, AgentMessage message)
        {
            if (Status != AgentStatus.Active)
            {
                return AgentResult.Failure(
                    "Agent is not active",
                    agentId: Id,
                    operationType: "Communicate");
            }

            try
            {
                return await OnCommunicateAsync(targetAgent, message);
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Communicate");
            }
        }

        public virtual async Task<AgentResult> LearnAsync(AgentExperience experience)
        {
            if (Status != AgentStatus.Active)
            {
                return AgentResult.Failure(
                    "Agent is not active",
                    agentId: Id,
                    operationType: "Learn");
            }

            try
            {
                _experiences.Add(experience);
                var result = await OnLearnAsync(experience);
                return result;
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Learn");
            }
        }

        public virtual async Task<AgentResult> ShutdownAsync()
        {
            try
            {
                Status = AgentStatus.Inactive;
                await OnShutdownAsync();
                return AgentResult.Success(
                    "Agent shutdown successfully",
                    agentId: Id,
                    operationType: "Shutdown");
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Shutdown");
            }
        }

        public virtual AgentState GetState()
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                _capabilities,
                _focusAreas,
                _configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                runtimeData: _runtimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                recentExperiences: _experiences.TakeLast(100).ToList());
        }

        public virtual async Task<AgentResult> RestoreAsync(AgentState state)
        {
            try
            {
                _capabilities.Clear();
                _capabilities.AddRange(state.Capabilities);

                _focusAreas.Clear();
                _focusAreas.AddRange(state.FocusAreas);

                _configuration.Clear();
                foreach (var kvp in state.Configuration)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }

                _runtimeData.Clear();
                foreach (var kvp in state.RuntimeData)
                {
                    _runtimeData[kvp.Key] = kvp.Value;
                }

                _experiences.Clear();
                _experiences.AddRange(state.RecentExperiences);

                Status = state.Status;
                await OnRestoreAsync(state);
                return AgentResult.Success(
                    "Agent state restored successfully",
                    agentId: Id,
                    operationType: "Restore");
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Restore");
            }
        }

        // Abstract methods to be implemented by derived classes
        protected abstract Task OnInitializeAsync(AgentContext context);
        protected abstract Task<AgentResult> OnExecuteAsync(AgentTask task);
        protected abstract Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message);
        protected abstract Task<AgentResult> OnLearnAsync(AgentExperience experience);
        protected abstract Task OnShutdownAsync();
        protected abstract Task OnRestoreAsync(AgentState state);

        // Helper methods
        protected void AddCapability(AgentCapability capability)
        {
            if (!_capabilities.Contains(capability))
            {
                _capabilities.Add(capability);
            }
        }

        protected void RemoveCapability(AgentCapability capability)
        {
            _capabilities.Remove(capability);
        }

        protected void AddFocusArea(string focusArea)
        {
            if (!_focusAreas.Contains(focusArea))
            {
                _focusAreas.Add(focusArea);
            }
        }

        protected void RemoveFocusArea(string focusArea)
        {
            _focusAreas.Remove(focusArea);
        }

        protected void SetConfiguration(string key, object value)
        {
            _configuration[key] = value;
        }

        protected T? GetConfiguration<T>(string key)
        {
            if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        protected void SetRuntimeData(string key, object value)
        {
            _runtimeData[key] = value;
        }

        protected T? GetRuntimeData<T>(string key)
        {
            if (_runtimeData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        protected void AddExperience(AgentExperience experience)
        {
            _experiences.Add(experience);
            // Keep only the most recent 1000 experiences
            if (_experiences.Count > 1000)
            {
                _experiences.RemoveAt(0);
            }
        }

        protected IEnumerable<AgentExperience> GetRecentExperiences(int count = 10)
        {
            return _experiences
                .OrderByDescending(e => e.Timestamp)
                .Take(count);
        }

        protected IEnumerable<AgentExperience> GetExperiencesByType(ExperienceType type, int count = 10)
        {
            return _experiences
                .Where(e => e.Type == type)
                .OrderByDescending(e => e.Timestamp)
                .Take(count);
        }

        protected IEnumerable<AgentExperience> GetPositiveExperiences(int count = 10)
        {
            return _experiences
                .Where(e => e.IsPositive)
                .OrderByDescending(e => e.Timestamp)
                .Take(count);
        }

        protected IEnumerable<AgentExperience> GetNegativeExperiences(int count = 10)
        {
            return _experiences
                .Where(e => e.IsNegative)
                .OrderByDescending(e => e.Timestamp)
                .Take(count);
        }
    }
}
