using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Context information provided to agents during initialization
    /// </summary>
    public sealed class AgentContext
    {
        public PlatformType Platform { get; }
        public string Environment { get; }
        public IReadOnlyDictionary<string, object> EnvironmentVariables { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }
        public IReadOnlyList<IAgent> AvailableAgents { get; }
        public IReadOnlyList<string> AvailableResources { get; }
        public AgentSecurityContext SecurityContext { get; }
        public DateTimeOffset Timestamp { get; }

        public AgentContext(
            PlatformType platform,
            string environment = "Development",
            IDictionary<string, object>? environmentVariables = null,
            IDictionary<string, object>? configuration = null,
            IEnumerable<IAgent>? availableAgents = null,
            IEnumerable<string>? availableResources = null,
            AgentSecurityContext? securityContext = null)
        {
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            EnvironmentVariables = (environmentVariables?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            Configuration = (configuration?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            AvailableAgents = (availableAgents?.ToList() ?? new List<IAgent>()).AsReadOnly();
            AvailableResources = (availableResources?.ToList() ?? new List<string>()).AsReadOnly();
            SecurityContext = securityContext ?? new AgentSecurityContext();
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Create a context for a specific platform
        /// </summary>
        public static AgentContext ForPlatform(PlatformType platform, string environment = "Development")
        {
            return new AgentContext(platform, environment);
        }

        /// <summary>
        /// Create a context with additional configuration
        /// </summary>
        public AgentContext WithConfiguration(IDictionary<string, object> additionalConfig)
        {
            var mergedConfig = new Dictionary<string, object>(Configuration);
            foreach (var kvp in additionalConfig)
            {
                mergedConfig[kvp.Key] = kvp.Value;
            }

            return new AgentContext(
                Platform,
                Environment,
                EnvironmentVariables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                mergedConfig,
                AvailableAgents,
                AvailableResources,
                SecurityContext);
        }

        /// <summary>
        /// Create a context with additional agents
        /// </summary>
        public AgentContext WithAgents(IEnumerable<IAgent> additionalAgents)
        {
            var allAgents = new List<IAgent>(AvailableAgents);
            allAgents.AddRange(additionalAgents);

            return new AgentContext(
                Platform,
                Environment,
                EnvironmentVariables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                allAgents,
                AvailableResources,
                SecurityContext);
        }
    }
}
