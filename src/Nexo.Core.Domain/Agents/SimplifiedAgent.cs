using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Simplified agent implementation following test structure principles:
    /// - Single responsibility operations
    /// - Simple state management
    /// - Clear success/failure patterns
    /// - No complex orchestration
    /// </summary>
    public abstract class SimplifiedAgent : IAgent
    {
        public AgentId Id { get; }
        public AgentName Name { get; }
        public AgentStatus Status { get; private set; }
        public PlatformType Platform { get; }
        public IReadOnlyList<AgentCapability> Capabilities { get; }
        public IReadOnlyList<string> FocusAreas { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }

        protected SimplifiedAgent(
            AgentId id, 
            AgentName name, 
            PlatformType platform,
            IReadOnlyList<AgentCapability> capabilities,
            IReadOnlyList<string> focusAreas,
            IReadOnlyDictionary<string, object> configuration)
        {
            Id = id;
            Name = name;
            Platform = platform;
            Capabilities = capabilities;
            FocusAreas = focusAreas;
            Configuration = configuration;
            Status = AgentStatus.Inactive;
        }

        /// <summary>
        /// Simple initialization (like test setup)
        /// </summary>
        public virtual async Task<AgentResult> InitializeAsync(AgentContext context)
        {
            try
            {
                Status = AgentStatus.Active;
                await OnInitializeAsync(context);
                return AgentResult.Success("Agent initialized successfully", agentId: Id, operationType: "Initialize");
            }
            catch (Exception ex)
            {
                Status = AgentStatus.Failed;
                return AgentResult.FromException(ex, Id, "Initialize");
            }
        }

        /// <summary>
        /// Simple task execution (like test execution)
        /// </summary>
        public virtual async Task<AgentResult> ExecuteAsync(AgentTask task)
        {
            try
            {
                var result = await OnExecuteAsync(task);
                return result;
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, task.Type.ToString());
            }
        }

        /// <summary>
        /// Simple communication (like test coordination)
        /// </summary>
        public virtual async Task<AgentResult> CommunicateAsync(IAgent targetAgent, AgentMessage message)
        {
            try
            {
                var result = await OnCommunicateAsync(targetAgent, message);
                return result;
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Communicate");
            }
        }

        /// <summary>
        /// Simple learning (like test result processing)
        /// </summary>
        public virtual async Task<AgentResult> LearnAsync(AgentExperience experience)
        {
            try
            {
                var result = await OnLearnAsync(experience);
                return result;
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Learn");
            }
        }

        /// <summary>
        /// Simple shutdown (like test cleanup)
        /// </summary>
        public virtual async Task<AgentResult> ShutdownAsync()
        {
            try
            {
                Status = AgentStatus.Inactive;
                await OnShutdownAsync();
                return AgentResult.Success("Agent shutdown successfully", agentId: Id, operationType: "Shutdown");
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Shutdown");
            }
        }

        /// <summary>
        /// Simple state management (like test state)
        /// </summary>
        public virtual AgentState GetState()
        {
            return new AgentState(
                Id,
                Name,
                Status,
                Platform,
                Capabilities,
                FocusAreas,
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                new List<string>(),
                new List<string>(),
                DateTimeOffset.UtcNow,
                1);
        }

        /// <summary>
        /// Simple state restoration (like test state restoration)
        /// </summary>
        public virtual async Task<AgentResult> RestoreAsync(AgentState state)
        {
            try
            {
                Status = state.Status;
                await OnRestoreAsync(state);
                return AgentResult.Success("Agent state restored successfully", agentId: Id, operationType: "Restore");
            }
            catch (Exception ex)
            {
                return AgentResult.FromException(ex, Id, "Restore");
            }
        }

        // Abstract methods for subclasses to implement (like test methods)
        protected abstract Task OnInitializeAsync(AgentContext context);
        protected abstract Task<AgentResult> OnExecuteAsync(AgentTask task);
        protected abstract Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message);
        protected abstract Task<AgentResult> OnLearnAsync(AgentExperience experience);
        protected abstract Task OnShutdownAsync();
        protected abstract Task OnRestoreAsync(AgentState state);
    }
}
