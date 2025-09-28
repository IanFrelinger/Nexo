using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents an experience that an agent can learn from
    /// </summary>
    public sealed class AgentExperience
    {
        public string Id { get; }
        public AgentId AgentId { get; }
        public ExperienceType Type { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, object> Context { get; }
        public IReadOnlyDictionary<string, object> Outcome { get; }
        public ExperienceQuality Quality { get; }
        public IReadOnlyList<string> LearnedCapabilities { get; }
        public IReadOnlyList<string> ImprovedSkills { get; }
        public DateTimeOffset Timestamp { get; }
        public TimeSpan Duration { get; }
        public IReadOnlyList<string> Tags { get; }

        public AgentExperience(
            string id,
            AgentId agentId,
            ExperienceType type,
            string description,
            ExperienceQuality quality,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            Type = type;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Quality = quality;
            Context = (context?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            Outcome = (outcome?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            LearnedCapabilities = (learnedCapabilities?.ToList() ?? new List<string>()).AsReadOnly();
            ImprovedSkills = (improvedSkills?.ToList() ?? new List<string>()).AsReadOnly();
            Timestamp = DateTimeOffset.UtcNow;
            Duration = duration ?? TimeSpan.Zero;
            Tags = (tags?.ToList() ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        /// Create a successful task completion experience
        /// </summary>
        public static AgentExperience CreateTaskCompletion(
            string id,
            AgentId agentId,
            string taskName,
            string taskDescription,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            return new AgentExperience(
                id,
                agentId,
                ExperienceType.TaskCompletion,
                $"Completed task: {taskName}",
                ExperienceQuality.Positive,
                context,
                outcome,
                learnedCapabilities,
                improvedSkills,
                duration,
                tags);
        }

        /// <summary>
        /// Create a failed task experience
        /// </summary>
        public static AgentExperience CreateTaskFailure(
            string id,
            AgentId agentId,
            string taskName,
            string failureReason,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            return new AgentExperience(
                id,
                agentId,
                ExperienceType.TaskFailure,
                $"Failed task: {taskName} - {failureReason}",
                ExperienceQuality.Negative,
                context,
                outcome,
                learnedCapabilities,
                improvedSkills,
                duration,
                tags);
        }

        /// <summary>
        /// Create a communication experience
        /// </summary>
        public static AgentExperience CreateCommunication(
            string id,
            AgentId agentId,
            AgentId otherAgentId,
            string communicationType,
            string description,
            ExperienceQuality quality,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            var fullContext = new Dictionary<string, object>(context ?? new Dictionary<string, object>())
            {
                ["otherAgentId"] = otherAgentId.ToString(),
                ["communicationType"] = communicationType
            };

            return new AgentExperience(
                id,
                agentId,
                ExperienceType.Communication,
                description,
                quality,
                fullContext,
                outcome,
                learnedCapabilities,
                improvedSkills,
                duration,
                tags);
        }

        /// <summary>
        /// Create a learning experience
        /// </summary>
        public static AgentExperience CreateLearning(
            string id,
            AgentId agentId,
            string learningTopic,
            string description,
            ExperienceQuality quality,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            var fullContext = new Dictionary<string, object>(context ?? new Dictionary<string, object>())
            {
                ["learningTopic"] = learningTopic
            };

            return new AgentExperience(
                id,
                agentId,
                ExperienceType.Learning,
                description,
                quality,
                fullContext,
                outcome,
                learnedCapabilities,
                improvedSkills,
                duration,
                tags);
        }

        /// <summary>
        /// Create a collaboration experience
        /// </summary>
        public static AgentExperience CreateCollaboration(
            string id,
            AgentId agentId,
            IEnumerable<AgentId> collaboratorIds,
            string collaborationType,
            string description,
            ExperienceQuality quality,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? outcome = null,
            IEnumerable<string>? learnedCapabilities = null,
            IEnumerable<string>? improvedSkills = null,
            TimeSpan? duration = null,
            IEnumerable<string>? tags = null)
        {
            var fullContext = new Dictionary<string, object>(context ?? new Dictionary<string, object>())
            {
                ["collaboratorIds"] = collaboratorIds.Select(id => id.ToString()).ToList(),
                ["collaborationType"] = collaborationType
            };

            return new AgentExperience(
                id,
                agentId,
                ExperienceType.Collaboration,
                description,
                quality,
                fullContext,
                outcome,
                learnedCapabilities,
                improvedSkills,
                duration,
                tags);
        }

        /// <summary>
        /// Get a context value
        /// </summary>
        public T? GetContextValue<T>(string key)
        {
            if (Context.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Get an outcome value
        /// </summary>
        public T? GetOutcomeValue<T>(string key)
        {
            if (Outcome.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Check if experience has learned capabilities
        /// </summary>
        public bool HasLearnedCapabilities => LearnedCapabilities.Count > 0;

        /// <summary>
        /// Check if experience has improved skills
        /// </summary>
        public bool HasImprovedSkills => ImprovedSkills.Count > 0;

        /// <summary>
        /// Check if experience is positive
        /// </summary>
        public bool IsPositive => Quality == ExperienceQuality.Positive;

        /// <summary>
        /// Check if experience is negative
        /// </summary>
        public bool IsNegative => Quality == ExperienceQuality.Negative;

        /// <summary>
        /// Check if experience is neutral
        /// </summary>
        public bool IsNeutral => Quality == ExperienceQuality.Neutral;

        /// <summary>
        /// Get a summary of the experience
        /// </summary>
        public string GetSummary()
        {
            var qualityText = Quality switch
            {
                ExperienceQuality.Positive => "Positive",
                ExperienceQuality.Negative => "Negative",
                ExperienceQuality.Neutral => "Neutral",
                _ => "Unknown"
            };

            var summary = $"{Type} experience ({qualityText})";
            if (HasLearnedCapabilities)
            {
                summary += $" - Learned: {string.Join(", ", LearnedCapabilities)}";
            }
            if (HasImprovedSkills)
            {
                summary += $" - Improved: {string.Join(", ", ImprovedSkills)}";
            }
            if (Duration > TimeSpan.Zero)
            {
                summary += $" - Duration: {Duration.TotalSeconds:F1}s";
            }

            return summary;
        }
    }

    /// <summary>
    /// Types of experiences that agents can have
    /// </summary>
    public enum ExperienceType
    {
        TaskCompletion,
        TaskFailure,
        Communication,
        Learning,
        Collaboration,
        ProblemSolving,
        ErrorRecovery,
        SkillDevelopment,
        KnowledgeAcquisition,
        SocialInteraction
    }

    /// <summary>
    /// Quality of an experience
    /// </summary>
    public enum ExperienceQuality
    {
        Positive,
        Negative,
        Neutral
    }
}
