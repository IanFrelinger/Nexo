using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Security context for agent operations
    /// </summary>
    public sealed class AgentSecurityContext
    {
        public SecurityLevel SecurityLevel { get; }
        public IReadOnlyList<string> AllowedCapabilities { get; }
        public IReadOnlyList<string> RestrictedCapabilities { get; }
        public IReadOnlyList<string> AllowedResources { get; }
        public IReadOnlyList<string> RestrictedResources { get; }
        public IReadOnlyList<string> AllowedAgents { get; }
        public IReadOnlyList<string> RestrictedAgents { get; }
        public bool RequireAuthentication { get; }
        public bool RequireEncryption { get; }
        public TimeSpan SessionTimeout { get; }

        public AgentSecurityContext(
            SecurityLevel securityLevel = SecurityLevel.Standard,
            IEnumerable<string>? allowedCapabilities = null,
            IEnumerable<string>? restrictedCapabilities = null,
            IEnumerable<string>? allowedResources = null,
            IEnumerable<string>? restrictedResources = null,
            IEnumerable<string>? allowedAgents = null,
            IEnumerable<string>? restrictedAgents = null,
            bool requireAuthentication = true,
            bool requireEncryption = true,
            TimeSpan? sessionTimeout = null)
        {
            SecurityLevel = securityLevel;
            AllowedCapabilities = (allowedCapabilities?.ToList() ?? new List<string>()).AsReadOnly();
            RestrictedCapabilities = (restrictedCapabilities?.ToList() ?? new List<string>()).AsReadOnly();
            AllowedResources = (allowedResources?.ToList() ?? new List<string>()).AsReadOnly();
            RestrictedResources = (restrictedResources?.ToList() ?? new List<string>()).AsReadOnly();
            AllowedAgents = (allowedAgents?.ToList() ?? new List<string>()).AsReadOnly();
            RestrictedAgents = (restrictedAgents?.ToList() ?? new List<string>()).AsReadOnly();
            RequireAuthentication = requireAuthentication;
            RequireEncryption = requireEncryption;
            SessionTimeout = sessionTimeout ?? TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Check if a capability is allowed
        /// </summary>
        public bool IsCapabilityAllowed(string capabilityName)
        {
            if (RestrictedCapabilities.Contains(capabilityName, StringComparer.OrdinalIgnoreCase))
                return false;

            if (AllowedCapabilities.Count == 0)
                return true; // No restrictions

            return AllowedCapabilities.Contains(capabilityName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a resource is allowed
        /// </summary>
        public bool IsResourceAllowed(string resourceName)
        {
            if (RestrictedResources.Contains(resourceName, StringComparer.OrdinalIgnoreCase))
                return false;

            if (AllowedResources.Count == 0)
                return true; // No restrictions

            return AllowedResources.Contains(resourceName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if communication with another agent is allowed
        /// </summary>
        public bool IsAgentCommunicationAllowed(string agentId)
        {
            if (RestrictedAgents.Contains(agentId, StringComparer.OrdinalIgnoreCase))
                return false;

            if (AllowedAgents.Count == 0)
                return true; // No restrictions

            return AllowedAgents.Contains(agentId, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Create a more restrictive security context
        /// </summary>
        public AgentSecurityContext WithRestrictions(
            IEnumerable<string>? additionalRestrictedCapabilities = null,
            IEnumerable<string>? additionalRestrictedResources = null,
            IEnumerable<string>? additionalRestrictedAgents = null)
        {
            var restrictedCaps = new List<string>(RestrictedCapabilities);
            restrictedCaps.AddRange(additionalRestrictedCapabilities ?? new List<string>());

            var restrictedRes = new List<string>(RestrictedResources);
            restrictedRes.AddRange(additionalRestrictedResources ?? new List<string>());

            var restrictedAgents = new List<string>(RestrictedAgents);
            restrictedAgents.AddRange(additionalRestrictedAgents ?? new List<string>());

            return new AgentSecurityContext(
                SecurityLevel,
                AllowedCapabilities,
                restrictedCaps,
                AllowedResources,
                restrictedRes,
                AllowedAgents,
                restrictedAgents,
                RequireAuthentication,
                RequireEncryption,
                SessionTimeout);
        }

        /// <summary>
        /// Create a less restrictive security context
        /// </summary>
        public AgentSecurityContext WithPermissions(
            IEnumerable<string>? additionalAllowedCapabilities = null,
            IEnumerable<string>? additionalAllowedResources = null,
            IEnumerable<string>? additionalAllowedAgents = null)
        {
            var allowedCaps = new List<string>(AllowedCapabilities);
            allowedCaps.AddRange(additionalAllowedCapabilities ?? new List<string>());

            var allowedRes = new List<string>(AllowedResources);
            allowedRes.AddRange(additionalAllowedResources ?? new List<string>());

            var allowedAgents = new List<string>(AllowedAgents);
            allowedAgents.AddRange(additionalAllowedAgents ?? new List<string>());

            return new AgentSecurityContext(
                SecurityLevel,
                allowedCaps,
                RestrictedCapabilities,
                allowedRes,
                RestrictedResources,
                allowedAgents,
                RestrictedAgents,
                RequireAuthentication,
                RequireEncryption,
                SessionTimeout);
        }
    }

    /// <summary>
    /// Security levels for agent operations
    /// </summary>
    public enum SecurityLevel
    {
        Minimal = 0,
        Standard = 1,
        High = 2,
        Maximum = 3
    }
}
